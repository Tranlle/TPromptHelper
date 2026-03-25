using TPromptHelper.Core.Models;

namespace TPromptHelper.Core.Interfaces;

/// <summary>
/// 优化会话仓储接口，管理提示词优化会话的持久化
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// 获取所有会话
    /// </summary>
    /// <returns>按修改时间降序排列的会话集合</returns>
    Task<IEnumerable<OptimizationSession>> GetAllAsync();

    /// <summary>
    /// 根据 ID 获取会话
    /// </summary>
    /// <param name="id">会话的唯一标识符</param>
    /// <returns>匹配的会话，如果不存在则返回 null</returns>
    Task<OptimizationSession?> GetByIdAsync(Guid id);

    /// <summary>
    /// 保存会话（新增或更新）
    /// </summary>
    /// <param name="session">要保存的会话</param>
    /// <remarks>
    /// 保存时会自动更新 <see cref="OptimizationSession.ModifiedAt"/> 为当前时间
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> 为 null</exception>
    Task SaveAsync(OptimizationSession session);

    /// <summary>
    /// 删除指定 ID 的会话
    /// </summary>
    /// <param name="id">要删除的会话 ID</param>
    Task DeleteAsync(Guid id);
}
