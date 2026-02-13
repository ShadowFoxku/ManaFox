using ManaFox.Core.Flow;
using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualCompositionTests
{
    [Fact]
    public void ComplexChain_MapBindScryRecover_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(2);
        var scryExecuted = false;

        // Act
        var result = ritual
            .Map(x => x * 3)                              // 6
            .Bind(x => Ritual<int>.Flow(x + 4))           // 10
            .Scry(x => scryExecuted = true)
            .Map(x => x * 2);                             // 20

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(20, result.GetValue());
        Assert.True(scryExecuted);
    }

    [Fact]
    public void ErrorPropagation_ThroughChain_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(5);

        // Act
        var result = ritual
            .Map(x => x * 2)                              // 10
            .Bind(x => Ritual<int>.Tear("Failed here"))
            .Map(x => x + 100)                            // This should not execute
            .Bind(x => Ritual<int>.Flow(x));              // This should not execute

        // Assert
        Assert.True(result.IsTorn);
        Assert.Equal("Failed here", result.GetTear()!.Message);
    }

    [Fact]
    public void RecoveryChain_WithMultipleAttempts_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("First attempt failed");

        // Act
        var result = ritual
            .Recover(tear => Ritual<int>.Tear("Second attempt failed"))
            .Recover(tear => Ritual<int>.Tear("Third attempt failed"))
            .Recover(tear => Ritual<int>.Flow(42));

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(42, result.GetValue());
    }

    [Fact]
    public async Task AsyncComposition_MapBindScryRecover_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(2);

        // Act
        var result = await ritual
            .BindAsync(async x =>
            {
                await Task.Delay(10);
                return Ritual<int>.Flow(x * 3);
            })
            .BindAsync(async x =>
            {
                await Task.Delay(10);
                return Ritual<int>.Flow(x + 4);
            });

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(10, result.GetValue());
    }

    [Fact]
    public void PipelineWithConditionalRecovery_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Timeout error", "TIMEOUT");

        // Act
        var result = ritual
            .Recover(tear => tear.Code == "TIMEOUT"
                ? Ritual<int>.Flow(0)
                : Ritual<int>.Tear(tear)
            )
            .Map(x => x + 10);

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(10, result.GetValue());
    }
}