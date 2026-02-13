using ManaFox.Core.Errors;
using ManaFox.Core.Flow;
using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualMapTests
{
    [Fact]
    public void Map_OnFlowingRitual_TransformsValue()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(5);

        // Act
        var result = ritual.Map(x => x * 2);

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(10, result.GetValue());
    }

    [Fact]
    public void Map_OnTornRitual_PreservesError()
    {
        // Arrange
        var tear = new Tear("Original error");
        var ritual = Ritual<int>.Tear(tear);

        // Act
        var result = ritual.Map(x => x * 2);

        // Assert
        Assert.True(result.IsTorn);
        Assert.Equal("Original error", result.GetTear()!.Message);
    }

    [Fact]
    public void Map_CanChainMultipleTransformations()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(2);

        // Act
        var result = ritual
            .Map(x => x * 2)    // 4
            .Map(x => x + 3)    // 7
            .Map(x => x * 10);  // 70

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(70, result.GetValue());
    }

    [Fact]
    public void Map_WithTypeChange_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(42);

        // Act
        var result = ritual.Map(x => x.ToString());

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal("42", result.GetValue());
    }
}
