using ManaFox.Core.Errors;

namespace ManaFox.Tests.TearTests;

public class TearTests
{
    [Fact]
    public void Tear_WithMessageOnly_CreatesValidTear()
    {
        // Arrange & Act
        var tear = new Tear("Error message");

        // Assert
        Assert.Equal("Error message", tear.Message);
        Assert.Null(tear.Code);
        Assert.Null(tear.InnerException);
    }

    [Fact]
    public void Tear_WithMessageAndCode_CreatesValidTear()
    {
        // Arrange & Act
        var tear = new Tear("Error message", "ERR_001");

        // Assert
        Assert.Equal("Error message", tear.Message);
        Assert.Equal("ERR_001", tear.Code);
        Assert.Null(tear.InnerException);
    }

    [Fact]
    public void Tear_WithAllParameters_CreatesValidTear()
    {
        // Arrange
        var exception = new InvalidOperationException("Inner error");

        // Act
        var tear = new Tear("Error message", "ERR_002", exception);

        // Assert
        Assert.Equal("Error message", tear.Message);
        Assert.Equal("ERR_002", tear.Code);
        Assert.NotNull(tear.InnerException);
        Assert.Same(exception, tear.InnerException);
    }

    [Fact]
    public void Tear_ToString_WithoutCode_FormatsCorrectly()
    {
        // Arrange
        var tear = new Tear("Something went wrong");

        // Act
        var result = tear.ToString();

        // Assert
        Assert.Equal("Something went wrong", result);
    }

    [Fact]
    public void Tear_ToString_WithCode_FormatsCorrectly()
    {
        // Arrange
        var tear = new Tear("Something went wrong", "ERR_500");

        // Act
        var result = tear.ToString();

        // Assert
        Assert.Equal("[ERR_500] Something went wrong", result);
    }

    [Fact]
    public void Tear_FromException_CapturesTourException()
    {
        // Arrange
        var exception = new TimeoutException("Request timed out");

        // Act
        var tear = Tear.FromException(exception);

        // Assert
        Assert.Equal("Request timed out", tear.Message);
        Assert.Null(tear.Code);
        Assert.NotNull(tear.InnerException);
        Assert.Same(exception, tear.InnerException);
    }

    [Fact]
    public void Tear_FromException_WithCode_CapturesException()
    {
        // Arrange
        var exception = new InvalidOperationException("Operation invalid");

        // Act
        var tear = Tear.FromException(exception, "ERR_OP");

        // Assert
        Assert.Equal("Operation invalid", tear.Message);
        Assert.Equal("ERR_OP", tear.Code);
        Assert.Same(exception, tear.InnerException);
    }
}
