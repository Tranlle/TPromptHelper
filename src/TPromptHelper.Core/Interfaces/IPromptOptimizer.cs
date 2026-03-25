using TPromptHelper.Core.Models;

namespace TPromptHelper.Core.Interfaces;

/// <summary>
/// 提示词优化器接口，根据不同策略优化用户输入
/// </summary>
public interface IPromptOptimizer
{
    /// <summary>
    /// 同步优化提示词
    /// </summary>
    /// <param name="rawInput">原始用户输入</param>
    /// <param name="strategy">优化策略</param>
    /// <param name="profile">使用的模型配置</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>优化后的提示词</returns>
    /// <exception cref="ArgumentException"><paramref name="rawInput"/> 为空或仅包含空白</exception>
    /// <exception cref="ArgumentNullException"><paramref name="profile"/> 为 null</exception>
    Task<string> OptimizeAsync(
        string rawInput,
        OptimizationStrategy strategy,
        ModelProfile profile,
        CancellationToken ct = default);

    /// <summary>
    /// 流式优化提示词
    /// </summary>
    /// <param name="rawInput">原始用户输入</param>
    /// <param name="strategy">优化策略</param>
    /// <param name="profile">使用的模型配置</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>优化后提示词的异步流</returns>
    /// <exception cref="ArgumentException"><paramref name="rawInput"/> 为空或仅包含空白</exception>
    /// <exception cref="ArgumentNullException"><paramref name="profile"/> 为 null</exception>
    IAsyncEnumerable<string> OptimizeStreamAsync(
        string rawInput,
        OptimizationStrategy strategy,
        ModelProfile profile,
        CancellationToken ct = default);
}
