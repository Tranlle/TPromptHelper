using TPromptHelper.Core.Models;

namespace TPromptHelper.Core.Interfaces;

/// <summary>
/// Token 使用记录仓储接口，管理 API 调用产生的用量统计
/// </summary>
public interface ITokenUsageRepository
{
    /// <summary>
    /// 保存一条 Token 使用记录
    /// </summary>
    /// <param name="record">要保存的用量记录</param>
    /// <exception cref="ArgumentNullException"><paramref name="record"/> 为 null</exception>
    Task SaveAsync(TokenUsageRecord record);

    /// <summary>
    /// 获取所有 Token 使用记录
    /// </summary>
    /// <returns>按时间降序排列的用量记录集合</returns>
    Task<IEnumerable<TokenUsageRecord>> GetAllAsync();

    /// <summary>
    /// 清空所有 Token 使用记录
    /// </summary>
    Task ClearAsync();
}
