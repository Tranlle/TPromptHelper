namespace TPromptHelper.Core.Models;

/// <summary>
/// AI 模型配置，包含 API 连接信息和计费参数
/// </summary>
public class ModelProfile
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 配置显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 提供商类型：openai/anthropic/qwen/kimi/ollama/custom
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// API 端点 URL（可选，填写后将覆盖默认端点）
    /// </summary>
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称，如 gpt-4o、claude-3-sonnet
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// 最大生成 Token 数
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// 生成随机性参数，范围 0-2
    /// </summary>
    public double Temperature { get; set; } = 1.0;

    /// <summary>
    /// AES-256-GCM 加密后的 API 密钥
    /// </summary>
    public string EncryptedApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 是否为默认选中的模型配置
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 货币符号，支持 ¥ 或 $
    /// </summary>
    public string Currency { get; set; } = "¥";

    /// <summary>
    /// 输入价格（每百万 Token）
    /// </summary>
    public double InputPricePer1M { get; set; } = 0;

    /// <summary>
    /// 输出价格（每百万 Token）
    /// </summary>
    public double OutputPricePer1M { get; set; } = 0;
}
