using ManaFox.Core.Errors;
using ManaFox.Core.Flow;
using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualEdgeCaseTests
{
    [Fact]
    public void NullValueHandling_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<string?>.Flow(null);

        // Act & Assert
        Assert.True(ritual.IsFlowing);
        Assert.Null(ritual.GetValue());
    }

    [Fact]
    public void MapWithException_InMapFunction_ThrowsException()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(5);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ritual.Map<int, int>(x => throw new InvalidOperationException("Map error"))
        );
    }

    [Fact]
    public void MultipleFlows_CreateIndependentRituals()
    {
        // Arrange & Act
        var ritual1 = Ritual<int>.Flow(10);
        var ritual2 = Ritual<int>.Flow(20);

        // Assert
        Assert.Equal(10, ritual1.GetValue());
        Assert.Equal(20, ritual2.GetValue());
    }

    [Fact]
    public void MultipleTears_CreateIndependentRituals()
    {
        // Arrange & Act
        var ritual1 = Ritual<int>.Tear("Error 1");
        var ritual2 = Ritual<int>.Tear("Error 2");

        // Assert
        Assert.Equal("Error 1", ritual1.GetTear()!.Message);
        Assert.Equal("Error 2", ritual2.GetTear()!.Message);
    }

    [Fact]
    public void RitualWithComplexType_WorksCorrectly()
    {
        // Arrange
        var obj = new { Name = "Test", Value = 42 };
        var ritual = Ritual<object>.Flow(obj);

        // Act
        var result = ritual.Map(x => x);

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Same(obj, result.GetValue());
    }

    [Fact]
    public void TearWithException_PreservesExceptionDetails()
    {
        // Arrange
        var innerException = new ArgumentException("Invalid argument");
        var tear = new Tear("Wrapped error", "ERR_ARG", innerException);

        // Act
        var ritual = Ritual<int>.Tear(tear);

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.NotNull(ritual.GetTear()!.InnerException);
        Assert.IsType<ArgumentException>(ritual.GetTear()!.InnerException);
        Assert.Equal("Invalid argument", ritual.GetTear()!.InnerException!.Message);
    }
}
