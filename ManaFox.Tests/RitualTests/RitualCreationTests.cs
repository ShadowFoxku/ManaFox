using ManaFox.Core.Errors;
using ManaFox.Core.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualCreationTests
{
    [Fact]
    public void Flow_WithValue_CreatesSuccessfulRitual()
    {
        // Arrange & Act
        var ritual = Ritual<int>.Flow(42);

        // Assert
        Assert.True(ritual.IsFlowing);
        Assert.False(ritual.IsTorn);
        Assert.Equal(42, ritual.GetValue());
        Assert.Null(ritual.GetTear());
    }

    [Fact]
    public void Tear_WithTearObject_CreatesFailedRitual()
    {
        // Arrange
        var tear = new Tear("Something went wrong");

        // Act
        var ritual = Ritual<int>.Tear(tear);

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.False(ritual.IsFlowing);
        Assert.NotNull(ritual.GetTear());
        Assert.Equal("Something went wrong", ritual.GetTear()!.Message);
    }

    [Fact]
    public void Tear_WithMessage_CreatesFailedRitualWithMessage()
    {
        // Arrange & Act
        var ritual = Ritual<string>.Tear("Error message");

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.Equal("Error message", ritual.GetTear()!.Message);
        Assert.Null(ritual.GetTear()!.Code);
    }

    [Fact]
    public void Tear_WithMessageAndCode_CreatesFailedRitualWithMessageAndCode()
    {
        // Arrange & Act
        var ritual = Ritual<string>.Tear("Error message", "ERR_001");

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.Equal("Error message", ritual.GetTear()!.Message);
        Assert.Equal("ERR_001", ritual.GetTear()!.Code);
    }

    [Fact]
    public void Try_WithSuccessfulOperation_ReturnsFlowingRitual()
    {
        // Arrange & Act
        var ritual = Ritual<int>.Try(() => 42);

        // Assert
        Assert.True(ritual.IsFlowing);
        Assert.Equal(42, ritual.GetValue());
    }

    [Fact]
    public void Try_WithExceptionThrowingOperation_ReturnsTornRitual()
    {
        // Arrange & Act
        var ritual = Ritual<int>.Try(() => throw new InvalidOperationException("Test error"));

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.NotNull(ritual.GetTear());
        Assert.Contains("Test error", ritual.GetTear()!.Message);
        Assert.NotNull(ritual.GetTear()!.InnerException);
    }

    [Fact]
    public async Task TryAsync_WithSuccessfulAsyncOperation_ReturnsFlowingRitual()
    {
        // Arrange & Act
        var ritual = await Ritual<int>.TryAsync(async () =>
        {
            await Task.Delay(10);
            return 42;
        });

        // Assert
        Assert.True(ritual.IsFlowing);
        Assert.Equal(42, ritual.GetValue());
    }

    [Fact]
    public async Task TryAsync_WithExceptionThrowingAsyncOperation_ReturnsTornRitual()
    {
        // Arrange & Act
        var ritual = await Ritual<int>.TryAsync(async () =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Async error");
        });

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.NotNull(ritual.GetTear());
        Assert.Contains("Async error", ritual.GetTear()!.Message);
    }
}
