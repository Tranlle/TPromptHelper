using TPromptHelper.Core.Models;

namespace TPromptHelper.Core.Interfaces;

/// <summary>
/// 模型配置仓储接口，管理 AI 模型的配置信息
/// </summary>
public interface IModelProfileRepository
{
    /// <summary>
    /// 获取所有模型配置
    /// </summary>
    /// <returns>按创建时间升序排列的模型配置集合</returns>
    Task<IEnumerable<ModelProfile>> GetAllAsync();

    /// <summary>
    /// 根据 ID 获取模型配置
    /// </summary>
    /// <param name="id">模型配置的唯一标识符</param>
    /// <returns>匹配的模型配置，如果不存在则返回 null</returns>
    Task<ModelProfile?> GetByIdAsync(Guid id);

    /// <summary>
    /// 保存模型配置（新增或更新）
    /// </summary>
    /// <param name="profile">要保存的模型配置</param>
    /// <remarks>
    /// 如果配置设置了 <see cref="ModelProfile.IsDefault"/> 为 true，
    /// 则会自动将该模型设为默认，并清除其他模型的默认标记
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="profile"/> 为 null</exception>
    Task SaveAsync(ModelProfile profile);

    /// <summary>
    /// 删除指定 ID 的模型配置
    /// </summary>
    /// <param name="id">要删除的模型配置 ID</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// 获取当前默认的模型配置
    /// </summary>
    /// <returns>标记为默认的模型配置，如果不存在则返回 null</returns>
    Task<ModelProfile?> GetDefaultAsync();
}
