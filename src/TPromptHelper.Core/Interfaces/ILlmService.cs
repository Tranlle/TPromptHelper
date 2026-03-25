using TPromptHelper.Core.Models;

namespace TPromptHelper.Core.Interfaces;

/// <summary>
/// LLM 服务接口，提供提示词优化和流式输出能力
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// 发送完整请求并获取单次响应
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userMessage">用户输入消息</param>
    /// <param name="profile">模型配置</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>模型生成的响应内容</returns>
    /// <exception cref="HttpRequestException">API 调用失败时抛出</exception>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        ModelProfile profile,
        CancellationToken ct = default);

    /// <summary>
    /// 发送流式请求并返回响应内容的异步流
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userMessage">用户输入消息</param>
    /// <param name="profile">模型配置</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>响应内容的异步流（每个元素为一个文本块）</returns>
    /// <exception cref="HttpRequestException">API 调用失败时抛出</exception>
    IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        ModelProfile profile,
        CancellationToken ct = default);
}
