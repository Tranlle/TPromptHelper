using System.Runtime.CompilerServices;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;

namespace TPromptHelper.Services;

/// <summary>
/// 提示词优化服务，根据指定策略调用 LLM 优化用户输入
/// </summary>
public sealed class PromptOptimizer : IPromptOptimizer
{
    private readonly ILlmService _llm;

    public PromptOptimizer(ILlmService llm)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
    }

    /// <inheritdoc />
    public Task<string> OptimizeAsync(
        string rawInput,
        OptimizationStrategy strategy,
        ModelProfile profile,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawInput);
        ArgumentNullException.ThrowIfNull(profile);

        return _llm.CompleteAsync(StrategyPrompts.Get(strategy), rawInput, profile, ct);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> OptimizeStreamAsync(
        string rawInput,
        OptimizationStrategy strategy,
        ModelProfile profile,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawInput);
        ArgumentNullException.ThrowIfNull(profile);

        return _llm.StreamAsync(StrategyPrompts.Get(strategy), rawInput, profile, ct);
    }
}
