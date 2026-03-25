using Moq;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;
using TPromptHelper.Services;
using Xunit;

namespace TPromptHelper.Tests;

public class PromptOptimizerTests
{
    private readonly Mock<ILlmService> _llmMock = new();
    private readonly PromptOptimizer _sut;
    private readonly ModelProfile _profile = new() { ModelName = "gpt-4o", Provider = "openai" };

    public PromptOptimizerTests()
    {
        _sut = new PromptOptimizer(_llmMock.Object);
    }

    [Theory]
    [InlineData(OptimizationStrategy.Structured)]
    [InlineData(OptimizationStrategy.FewShot)]
    [InlineData(OptimizationStrategy.ChainOfThought)]
    [InlineData(OptimizationStrategy.Concise)]
    [InlineData(OptimizationStrategy.Technical)]
    public async Task OptimizeAsync_CallsLlmWithCorrectSystemPrompt(OptimizationStrategy strategy)
    {
        const string rawInput = "帮我写代码";
        const string expected = "优化后的提示词";

        _llmMock.Setup(x => x.CompleteAsync(It.IsAny<string>(), rawInput, _profile, default))
                .ReturnsAsync(expected);

        var result = await _sut.OptimizeAsync(rawInput, strategy, _profile);

        Assert.Equal(expected, result);
        _llmMock.Verify(x => x.CompleteAsync(
            It.Is<string>(s => s.Length > 10), // system prompt is non-trivial
            rawInput, _profile, default), Times.Once);
    }
}
