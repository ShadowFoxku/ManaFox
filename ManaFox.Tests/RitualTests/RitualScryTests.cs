using ManaFox.Core.Errors;
using ManaFox.Core.Flow;
using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualScryTests
{
    [Fact]
    public void Scry_OnFlowingRitual_ExecutesSideEffect()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);
        var sideEffectExecuted = false;
        var capturedValue = 0;

        // Act
        var result = ritual.Scry(x =>
        {
            sideEffectExecuted = true;
            capturedValue = x;
        });

        // Assert
        Assert.True(sideEffectExecuted);
        Assert.Equal(42, capturedValue);
        Assert.True(result.IsFlowing);
        Assert.Equal(42, result.GetValue());
    }

    [Fact]
    public void Scry_OnTornRitual_DoesNotExecuteSideEffect()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Error");
        var sideEffectExecuted = false;

        // Act
        var result = ritual.Scry(x => sideEffectExecuted = true);

        // Assert
        Assert.False(sideEffectExecuted);
        Assert.True(result.IsTorn);
    }

    [Fact]
    public async Task ScryAsync_OnFlowingRitual_ExecutesSideEffect()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);
        var sideEffectExecuted = false;
        var capturedValue = 0;

        // Act
        var result = await ritual.ScryAsync(async x =>
        {
            await Task.Delay(10);
            sideEffectExecuted = true;
            capturedValue = x;
        });

        // Assert
        Assert.True(sideEffectExecuted);
        Assert.Equal(42, capturedValue);
        Assert.True(result.IsFlowing);
    }

    [Fact]
    public void ScryTear_OnTornRitual_ExecutesSideEffect()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Error code", "ERR_001");
        var sideEffectExecuted = false;
        Tear? capturedTear = null;

        // Act
        var result = ritual.ScryTear(tear =>
        {
            sideEffectExecuted = true;
            capturedTear = tear;
        });

        // Assert
        Assert.True(sideEffectExecuted);
        Assert.NotNull(capturedTear);
        Assert.Equal("Error code", capturedTear!.Message);
        Assert.Equal("ERR_001", capturedTear.Code);
        Assert.True(result.IsTorn);
    }

    [Fact]
    public void ScryTear_OnFlowingRitual_DoesNotExecuteSideEffect()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);
        var sideEffectExecuted = false;

        // Act
        var result = ritual.ScryTear(tear => sideEffectExecuted = true);

        // Assert
        Assert.False(sideEffectExecuted);
        Assert.True(result.IsFlowing);
    }

    [Fact]
    public async Task ScryTearAsync_OnTornRitual_ExecutesSideEffect()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Async error");
        var sideEffectExecuted = false;
        string? capturedMessage = null;

        // Act
        var result = await ritual.ScryTearAsync(async tear =>
        {
            await Task.Delay(10);
            sideEffectExecuted = true;
            capturedMessage = tear.Message;
        });

        // Assert
        Assert.True(sideEffectExecuted);
        Assert.Equal("Async error", capturedMessage);
        Assert.True(result.IsTorn);
    }

    [Fact]
    public void Scry_CanChainMultipleSideEffects()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);
        var effects = new List<string>();

        // Act
        var result = ritual
            .Scry(x => effects.Add($"Effect1: {x}"))
            .Scry(x => effects.Add($"Effect2: {x}"))
            .Scry(x => effects.Add($"Effect3: {x}"));

        // Assert
        Assert.Equal(3, effects.Count);
        Assert.Equal("Effect1: 42", effects[0]);
        Assert.Equal("Effect2: 42", effects[1]);
        Assert.Equal("Effect3: 42", effects[2]);
        Assert.True(result.IsFlowing);
    }
}