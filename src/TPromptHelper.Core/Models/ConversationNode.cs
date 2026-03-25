namespace TPromptHelper.Core.Models;

public class ConversationNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public string RawInput { get; set; } = string.Empty;
    public string OptimizedOutput { get; set; } = string.Empty;
    public OptimizationStrategy Strategy { get; set; }
    public Guid ModelProfileId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ConversationNode> Children { get; set; } = [];
}
