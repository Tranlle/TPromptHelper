using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;

namespace TPromptHelper.Services;

/// <summary>
/// LLM 服务实现，支持 OpenAI 兼容 API 的同步和流式调用
/// </summary>
public sealed class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly IEncryptionService _encryption;
    private readonly IApiLogger _logger;
    private readonly ITokenUsageRepository _tokenUsageRepo;

    private static readonly JsonSerializerOptions LogJsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public LlmService(
        HttpClient httpClient,
        IEncryptionService encryption,
        IApiLogger logger,
        ITokenUsageRepository tokenUsageRepo)
    {
        _httpClient = httpClient;
        _encryption = encryption;
        _logger = logger;
        _tokenUsageRepo = tokenUsageRepo;
    }

    /// <inheritdoc />
    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        ModelProfile profile,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var entry = new ApiLogEntry
        {
            Provider = profile.Provider,
            ModelName = profile.ModelName,
            SystemPrompt = systemPrompt,
            UserInput = userMessage,
        };

        try
        {
            var requestObj = BuildRequest(systemPrompt, userMessage, profile, stream: false);
            entry.RequestUrl = GetEndpoint(profile);
            entry.RequestBody = JsonSerializer.Serialize(requestObj, LogJsonOpts);

            var response = await SendAsync(entry.RequestUrl, entry.RequestBody, profile, ct);
            var rawJson = await response.Content.ReadAsStringAsync(ct);
            entry.ResponseBody = FormatJson(rawJson);

            entry.Response = ExtractContent(rawJson);
            entry.IsSuccess = true;
            await SaveTokenUsageAsync(entry);
            return entry.Response;
        }
        catch (Exception ex)
        {
            entry.IsSuccess = false;
            entry.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            sw.Stop();
            entry.Duration = sw.Elapsed;
            _logger.Log(entry);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        ModelProfile profile,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var fullResponse = new StringBuilder();
        var entry = new ApiLogEntry
        {
            Provider = profile.Provider,
            ModelName = profile.ModelName,
            SystemPrompt = systemPrompt,
            UserInput = userMessage,
        };

        try
        {
            var requestObj = BuildRequest(systemPrompt, userMessage, profile, stream: true);
            entry.RequestUrl = GetEndpoint(profile);
            entry.RequestBody = JsonSerializer.Serialize(requestObj, LogJsonOpts);

            var response = await SendAsync(entry.RequestUrl, entry.RequestBody, profile, ct);
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);
            var sseLines = new StringBuilder();

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break;

                if (!string.IsNullOrEmpty(line))
                    sseLines.AppendLine(line);

                if (!line.StartsWith("data: ", StringComparison.Ordinal))
                    continue;

                var data = line["data: ".Length..];
                if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                    break;

                // 解析 SSE data 行
                if (!TryParseSseData(data, out var chunkRoot))
                    continue;

                // usage chunk：先处理 usage 再跳过内容解析
                if (chunkRoot.TryGetProperty("usage", out _))
                {
                    ApplyUsage(entry, chunkRoot, profile);
                    continue;
                }

                var choices = chunkRoot.GetProperty("choices");
                if (choices.GetArrayLength() == 0) continue;

                if (!choices[0].TryGetProperty("delta", out var delta))
                    continue;

                if (delta.TryGetProperty("content", out var content)
                    && content.ValueKind == JsonValueKind.String)
                {
                    var chunk = content.GetString() ?? string.Empty;
                    fullResponse.Append(chunk);
                    yield return chunk;
                }
            }

            entry.IsSuccess = true;
            entry.ResponseBody = sseLines.ToString();
        }
        finally
        {
            sw.Stop();
            entry.Response = fullResponse.ToString();
            entry.Duration = sw.Elapsed;
            _logger.Log(entry);
            if (entry.IsSuccess)
                await SaveTokenUsageAsync(entry);
        }
    }

    private static object BuildRequest(string systemPrompt, string userMessage, ModelProfile profile, bool stream)
    {
        if (stream)
        {
            return new
            {
                model = profile.ModelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                max_tokens = profile.MaxTokens,
                temperature = profile.Temperature,
                stream = true,
                stream_options = new { include_usage = true }
            };
        }
        return new
        {
            model = profile.ModelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            max_tokens = profile.MaxTokens,
            temperature = profile.Temperature,
            stream = false
        };
    }

    private async Task<HttpResponseMessage> SendAsync(
        string endpoint,
        string body,
        ModelProfile profile,
        CancellationToken ct)
    {
        // 空 EncryptedApiKey 直接传空字符串（Ollama 等无需 Key 的本地服务）
        var apiKey = string.IsNullOrEmpty(profile.EncryptedApiKey)
            ? string.Empty
            : _encryption.Decrypt(profile.EncryptedApiKey);

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}",
                null, response.StatusCode);
        }
        return response;
    }

    private async Task SaveTokenUsageAsync(ApiLogEntry entry)
    {
        try
        {
            await _tokenUsageRepo.SaveAsync(new TokenUsageRecord
            {
                Provider = entry.Provider,
                ModelName = entry.ModelName,
                PromptTokens = entry.PromptTokens,
                CompletionTokens = entry.CompletionTokens,
                TotalTokens = entry.TotalTokens,
                EstimatedCost = entry.EstimatedCost,
                Currency = entry.Currency
            });
        }
        catch (Exception ex)
        {
            // 记录到诊断日志，但不影响主流程
            System.Diagnostics.Debug.WriteLine(
                $"[TokenUsage] Failed to save: {ex.Message}");
        }
    }

    private static void ApplyUsage(ApiLogEntry entry, JsonElement root, ModelProfile profile)
    {
        if (!root.TryGetProperty("usage", out var usage)) return;
        entry.PromptTokens = usage.TryGetProperty("prompt_tokens", out var p) ? p.GetInt32() : 0;
        entry.CompletionTokens = usage.TryGetProperty("completion_tokens", out var c) ? c.GetInt32() : 0;
        entry.TotalTokens = usage.TryGetProperty("total_tokens", out var t) ? t.GetInt32() : entry.PromptTokens + entry.CompletionTokens;
        entry.Currency = profile.Currency;
        if (profile.InputPricePer1M > 0 || profile.OutputPricePer1M > 0)
            entry.EstimatedCost = (entry.PromptTokens * profile.InputPricePer1M + entry.CompletionTokens * profile.OutputPricePer1M) / 1_000_000.0;
    }

    private static string ExtractContent(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return string.Empty;

            if (!root.TryGetProperty("choices", out var choices))
                return string.Empty;

            if (choices.GetArrayLength() == 0)
                return string.Empty;

            var choice = choices[0];
            if (!choice.TryGetProperty("message", out var message))
                return string.Empty;

            if (!message.TryGetProperty("content", out var content))
                return string.Empty;

            return content.ValueKind == JsonValueKind.String
                ? content.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static bool TryParseSseData(string data, out JsonElement root)
    {
        root = default;
        try
        {
            root = JsonDocument.Parse(data).RootElement;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string FormatJson(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return JsonSerializer.Serialize(doc, LogJsonOpts);
        }
        catch
        {
            return raw;
        }
    }

    private static string GetEndpoint(ModelProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.ApiEndpoint))
        {
            var ep = profile.ApiEndpoint.TrimEnd('/');
            // 支持填写 Base URL（如 https://api.moonshot.cn/v1），自动补全路径
            if (!ep.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                ep += "/chat/completions";
            return ep;
        }

        return string.Equals(profile.Provider, "openai", StringComparison.OrdinalIgnoreCase)
            ? "https://api.openai.com/v1/chat/completions"
            : string.Equals(profile.Provider, "anthropic", StringComparison.OrdinalIgnoreCase)
                ? "https://api.anthropic.com/v1/messages"
                : string.Equals(profile.Provider, "qwen", StringComparison.OrdinalIgnoreCase)
                    ? "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions"
                    : string.Equals(profile.Provider, "kimi", StringComparison.OrdinalIgnoreCase)
                        ? "https://api.moonshot.cn/v1/chat/completions"
                        : string.Equals(profile.Provider, "ollama", StringComparison.OrdinalIgnoreCase)
                            ? "http://localhost:11434/v1/chat/completions"
                            : throw new InvalidOperationException(
                                $"未知提供商：{profile.Provider}，请在模型设置中手动填写 API 端点。");
    }
}
