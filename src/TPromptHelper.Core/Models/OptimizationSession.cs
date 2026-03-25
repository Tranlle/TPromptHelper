namespace TPromptHelper.Core.Models;

/// <summary>
/// 优化会话，存储一组相关的提示词优化历史
/// </summary>
public class OptimizationSession
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 会话显示标题
    /// </summary>
    public string Title { get; set; } = "新会话";

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后修改时间（UTC）
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 对话树结构
    /// </summary>
    public List<ConversationNode> ConversationTree { get; set; } = [];

    /// <summary>
    /// 当前选中的模型配置 ID
    /// </summary>
    public Guid? CurrentModelProfileId { get; set; }

    /// <summary>
    /// 默认优化策略
    /// </summary>
    public OptimizationStrategy DefaultStrategy { get; set; } = OptimizationStrategy.Structured;
}
