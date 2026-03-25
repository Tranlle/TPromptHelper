using TPromptHelper.Core.Models;

namespace TPromptHelper.Core.Interfaces;

/// <summary>
/// API 日志记录器接口，用于记录和查询 API 调用日志
/// </summary>
public interface IApiLogger
{
    /// <summary>
    /// 记录一条 API 日志条目
    /// </summary>
    /// <param name="entry">要记录的日志条目</param>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> 为 null</exception>
    void Log(ApiLogEntry entry);

    /// <summary>
    /// 清空所有日志记录
    /// </summary>
    void Clear();

    /// <summary>
    /// 获取所有日志条目的只读列表
    /// </summary>
    /// <remarks>
    /// 返回的列表按时间倒序排列（最新在前）
    /// </remarks>
    IReadOnlyList<ApiLogEntry> Entries { get; }

    /// <summary>
    /// 当日志集合发生变化时触发
    /// </summary>
    event Action? LogChanged;
}
