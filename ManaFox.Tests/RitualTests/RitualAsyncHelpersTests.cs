using ManaFox.Core.Errors;
using ManaFox.Core.Flow;
using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualAsyncHelpersTests
{
    [Fact]
    public async Task ScryAsync_WithAsyncRitualInput_ExecutesSideEffect()
    {
        // Arrange
        var ritualTask = Task.FromResult(Ritual<int>.Flow(42));
        var sideEffectExecuted = false;
        var capturedValue = 0;

        // Act
        var result = await ritualTask.ScryAsync(async x =>
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
    public async Task ScryAsync_OnTornRitual_WithAsyncRitualInput_DoesNotExecuteSideEffect()
    {
        // Arrange
        var ritualTask = Task.FromResult(Ritual<int>.Tear("Error"));
        var sideEffectExecuted = false;

        // Act
        var result = await ritualTask.ScryAsync(async x =>
        {
            await Task.Delay(10);
            sideEffectExecuted = true;
        });

        // Assert
        Assert.False(sideEffectExecuted);
        Assert.True(result.IsTorn);
    }

    [Fact]
    public async Task ScryTearAsync_WithAsyncRitualInput_ExecutesSideEffect()
    {
        // Arrange
        var ritualTask = Task.FromResult(Ritual<int>.Tear("Error", "ERR_001"));
        var sideEffectExecuted = false;
        Tear? capturedTear = null;

        // Act
        var result = await ritualTask.ScryTearAsync(async tear =>
        {
            await Task.Delay(10);
            sideEffectExecuted = true;
            capturedTear = tear;
        });

        // Assert
        Assert.True(sideEffectExecuted);
        Assert.NotNull(capturedTear);
        Assert.Equal("Error", capturedTear!.Message);
        Assert.Equal("ERR_001", capturedTear.Code);
        Assert.True(result.IsTorn);
    }

    [Fact]
    public async Task ScryTearAsync_OnFlowingRitual_WithAsyncRitualInput_DoesNotExecuteSideEffect()
    {
        // Arrange
        var ritualTask = Task.FromResult(Ritual<int>.Flow(42));
        var sideEffectExecuted = false;

        // Act
        var result = await ritualTask.ScryTearAsync(async tear =>
        {
            await Task.Delay(10);
            sideEffectExecuted = true;
        });

        // Assert
        Assert.False(sideEffectExecuted);
        Assert.True(result.IsFlowing);
    }
}