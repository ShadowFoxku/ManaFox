using ManaFox.Core.Errors;

namespace ManaFox.Tests.TearTests;

public class TearExceptionTests
{
    [Fact]
    public void TearException_WithMessageAndTear_CreatesValidException()
    {
        // Arrange
        var tear = new Tear("Original error", "ERR_001");

        // Act
        var exception = new TearException("Something failed", tear);

        // Assert
        Assert.Equal("Something failed", exception.Message);
        Assert.NotNull(exception.Tear);
        Assert.Same(tear, exception.Tear);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void TearException_WithMessageTearAndInner_CreatesValidException()
    {
        // Arrange
        var tear = new Tear("Original error", "ERR_002");
        var innerException = new InvalidOperationException("Underlying cause");

        // Act
        var exception = new TearException("Operation failed", tear, innerException);

        // Assert
        Assert.Equal("Operation failed", exception.Message);
        Assert.NotNull(exception.Tear);
        Assert.Same(tear, exception.Tear);
        Assert.NotNull(exception.InnerException);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void TearException_CanBeThrownAndCaught()
    {
        // Arrange
        var tear = new Tear("Test error", "ERR_TEST");

        // Act & Assert
        try
        {
            throw new TearException("Caught error", tear);
        }
        catch (TearException ex)
        {
            Assert.Equal("Caught error", ex.Message);
            Assert.Equal("Test error", ex.Tear.Message);
            Assert.Equal("ERR_TEST", ex.Tear.Code);
        }
    }
}