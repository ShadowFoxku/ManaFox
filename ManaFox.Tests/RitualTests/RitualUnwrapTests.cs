using ManaFox.Core.Errors;
using ManaFox.Core.Flow;
using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualUnwrapTests
{
    [Fact]
    public void OrElse_OnFlowingRitual_ReturnsValue()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);

        // Act
        var result = ritual.OrElse(99);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void OrElse_OnTornRitual_ReturnsFallback()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Error");

        // Act
        var result = ritual.OrElse(99);

        // Assert
        Assert.Equal(99, result);
    }

    [Fact]
    public void OrElse_WithFunction_OnFlowingRitual_ReturnsValue()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);

        // Act
        var result = ritual.OrElse(tear => 99);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void OrElse_WithFunction_OnTornRitual_ComputesFallback()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Error code", "ERR_001");

        // Act
        var result = ritual.OrElse(tear => tear.Code == "ERR_001" ? 100 : 200);

        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    public async Task OrElseAsync_OnFlowingRitual_ReturnsValue()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);

        // Act
        var result = await ritual.OrElseAsync(async tear =>
        {
            await Task.Delay(10);
            return 99;
        });

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task OrElseAsync_OnTornRitual_ComputesAsyncFallback()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Error");

        // Act
        var result = await ritual.OrElseAsync(async tear =>
        {
            await Task.Delay(10);
            return 99;
        });

        // Assert
        Assert.Equal(99, result);
    }

    [Fact]
    public void Expect_OnFlowingRitual_ReturnsValue()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);

        // Act
        var result = ritual.Expect("Value should exist");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Expect_OnTornRitual_ThrowsException()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Internal error");

        // Act & Assert
        var ex = Assert.Throws<TearException>(() =>
            ritual.Expect("Value should exist")
        );
        Assert.Contains("Value should exist", ex.Message);
        Assert.Contains("Internal error", ex.Message);
    }

    [Fact]
    public void Unwrap_OnFlowingRitual_ReturnsValue()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);

        // Act
        var result = ritual.Unwrap();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Unwrap_OnTornRitual_ThrowsException()
    {
        // Arrange
        var ritual = Ritual<int>.Tear("Something failed");

        // Act & Assert
        var ex = Assert.Throws<TearException>(() => ritual.Unwrap());
        Assert.Contains("Something failed", ex.Message);
        Assert.NotNull(ex.Tear);
        Assert.Equal("Something failed", ex.Tear.Message);
    }

    [Fact]
    public async Task UnwrapAsync_OnFlowingRitual_ReturnsValue()
    {
        // Arrange
        var ritualTask = Task.FromResult(Ritual<int>.Flow(42));

        // Act
        var result = await ritualTask.UnwrapAsync();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task UnwrapAsync_OnTornRitual_ThrowsException()
    {
        // Arrange
        var ritualTask = Task.FromResult(Ritual<int>.Tear("Async failed"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TearException>(() => ritualTask.UnwrapAsync());
        Assert.Contains("Async failed", ex.Message);
    }
}