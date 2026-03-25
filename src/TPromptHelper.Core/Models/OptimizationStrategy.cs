namespace TPromptHelper.Core.Models;

public enum OptimizationStrategy
{
    Structured,     // 结构化：角色定义+任务描述+约束条件
    FewShot,        // 少样本：自动补充示例
    ChainOfThought, // 思维链：引导分步推理
    Concise,        // 精简版：去冗余保核心
    Technical       // 技术向：代码规范+输出格式
}
