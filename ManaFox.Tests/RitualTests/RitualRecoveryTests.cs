using ManaFox.Core.Flow;
using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;


public class RitualRecoveryTests
{
    [Fact]
    public void Recover_OnTornRitual_ExecutesRecovery()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Initial error");

        // Act
        var result = ritual.Recover(tear => Ritual<int>.Flow(99));

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(99, result.GetValue());
    }

    [Fact]
    public void Recover_OnFlowingRitual_DoesNotExecuteRecovery()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);
        var recoveryWasCalled = false;

        // Act
        var result = ritual.Recover(tear =>
        {
            recoveryWasCalled = true;
            return Ritual<int>.Flow(99);
        });

        // Assert
        Assert.False(recoveryWasCalled);
        Assert.True(result.IsFlowing);
        Assert.Equal(42, result.GetValue());
    }

    [Fact]
    public void Recover_CanAccessTearInRecovery()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Original error", "ERR_001");

        // Act
        var result = ritual.Recover(tear =>
            Ritual<int>.Flow(tear.Code == "ERR_001" ? 100 : 200)
        );

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(100, result.GetValue());
    }

    [Fact]
    public async Task RecoverAsync_OnTornRitual_ExecutesAsyncRecovery()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Error");

        // Act
        var result = await ritual.RecoverAsync(async tear =>
        {
            await Task.Delay(10);
            return Ritual<int>.Flow(99);
        });

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(99, result.GetValue());
    }

    [Fact]
    public async Task RecoverAsync_WithAsyncRitualInput_WorksCorrectly()
    {
        // Arrange
        var ritualTask = Task.FromResult(Ritual<int>.Tear("Error"));

        // Act
        var result = await ritualTask.RecoverAsync(async tear =>
        {
            await Task.Delay(10);
            return Ritual<int>.Flow(99);
        });

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(99, result.GetValue());
    }

    [Fact]
    public void Recover_CanRecoverFromMultipleErrors()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("First error")
            .Recover(tear => Ritual<int>.Tear("Second error"))
            .Recover(tear => Ritual<int>.Tear("Third error"));

        // Act
        var result = ritual.Recover(tear => Ritual<int>.Flow(0));

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(0, result.GetValue());
    }
}