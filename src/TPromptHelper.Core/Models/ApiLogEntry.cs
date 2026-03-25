namespace TPromptHelper.Core.Models;

/// <summary>
/// API 调用日志条目，用于调试和审计
/// </summary>
public class ApiLogEntry
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 调用时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// API 提供商
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 使用的模型名称
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// 系统提示词
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 用户输入
    /// </summary>
    public string UserInput { get; set; } = string.Empty;

    /// <summary>
    /// 请求 URL
    /// </summary>
    public string RequestUrl { get; set; } = string.Empty;

    /// <summary>
    /// 请求体（格式化后的 JSON）
    /// </summary>
    public string RequestBody { get; set; } = string.Empty;

    /// <summary>
    /// 响应体（格式化后的 JSON）
    /// </summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>
    /// 提取的响应内容
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// 请求耗时
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 输入 Token 数量
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// 输出 Token 数量
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// 总 Token 数量
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// 估算费用（null 表示未配置价格）
    /// </summary>
    public double? EstimatedCost { get; set; }

    /// <summary>
    /// 货币符号
    /// </summary>
    public string Currency { get; set; } = string.Empty;
}
