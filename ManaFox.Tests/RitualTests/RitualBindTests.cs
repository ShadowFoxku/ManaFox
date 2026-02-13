using ManaFox.Core.Flow;
using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualBindTests
{
    [Fact]
    public void Bind_OnFlowingRitual_WithSuccessfulBind_ReturnsFlowingRitual()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(5);

        // Act
        var result = ritual.Bind(x => Ritual<int>.Flow(x * 2));

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(10, result.GetValue());
    }

    [Fact]
    public void Bind_OnFlowingRitual_WithFailingBind_ReturnsTornRitual()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(5);

        // Act
        var result = ritual.Bind(x => Ritual<int>.Tear("Bind failed"));

        // Assert
        Assert.True(result.IsTorn);
        Assert.Equal("Bind failed", result.GetTear()!.Message);
    }

    [Fact]
    public void Bind_OnTornRitual_ShortCircuits()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Initial error");
        var bindWasCalled = false;

        // Act
        var result = ritual.Bind(x =>
        {
            bindWasCalled = true;
            return Ritual<int>.Flow(x * 2);
        });

        // Assert
        Assert.True(result.IsTorn);
        Assert.False(bindWasCalled);
        Assert.Equal("Initial error", result.GetTear()!.Message);
    }

    [Fact]
    public void Bind_CanChainMultipleOperations()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(2);

        // Act
        var result = ritual
            .Bind(x => Ritual<int>.Flow(x * 2))      // 4
            .Bind(x => Ritual<int>.Flow(x + 3))      // 7
            .Bind(x => Ritual<int>.Flow(x * 10));    // 70

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(70, result.GetValue());
    }

    [Fact]
    public void Bind_WithTypeChange_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);

        // Act
        var result = ritual.Bind(x => Ritual<string>.Flow(x.ToString()));

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal("42", result.GetValue());
    }

    [Fact]
    public async Task BindAsync_WithAsyncOperation_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(5);

        // Act
        var result = await ritual.BindAsync(async x =>
        {
            await Task.Delay(10);
            return Ritual<int>.Flow(x * 2);
        });

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(10, result.GetValue());
    }

    [Fact]
    public async Task BindAsync_WithAsyncRitualInput_WorksCorrectly()
    {
        // Arrange
        var ritualTask = Task.FromResult(Ritual<int>.Flow(5));

        // Act
        var result = await ritualTask.BindAsync(x => Ritual<int>.Flow(x * 2));

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(10, result.GetValue());
    }

    [Fact]
    public async Task BindAsync_WithBothAsync_WorksCorrectly()
    {
        // Arrange
        var ritualTask = Task.FromResult(Ritual<int>.Flow(5));

        // Act
        var result = await ritualTask.BindAsync(async x =>
        {
            await Task.Delay(10);
            return Ritual<int>.Flow(x * 2);
        });

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(10, result.GetValue());
    }
}
