using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualConversionTests
{
    [Fact]
    public void ToRitual_WithNonNullReferenceType_CreatesFlowingRitual()
    {
        // Arrange
        string value = "test";

        // Act
        var ritual = value.ToRitual();

        // Assert
        Assert.True(ritual.IsFlowing);
        Assert.Equal("test", ritual.GetValue());
    }

    [Fact]
    public void ToRitual_WithNullReferenceType_CreatesTornRitual()
    {
        // Arrange
        string? value = null;

        // Act
        var ritual = value.ToRitual();

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.Equal("Value was null", ritual.GetTear()!.Message);
    }

    [Fact]
    public void ToRitual_WithNullReferenceType_CustomMessage_CreatesTornRitualWithCustomMessage()
    {
        // Arrange
        string? value = null;

        // Act
        var ritual = value.ToRitual("Custom error message");

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.Equal("Custom error message", ritual.GetTear()!.Message);
    }

    [Fact]
    public void ToRitual_WithNonNullValueType_CreatesFlowingRitual()
    {
        // Arrange
        int? value = 42;

        // Act
        var ritual = value.ToRitual();

        // Assert
        Assert.True(ritual.IsFlowing);
        Assert.Equal(42, ritual.GetValue());
    }

    [Fact]
    public void ToRitual_WithNullValueType_CreatesTornRitual()
    {
        // Arrange
        int? value = null;

        // Act
        var ritual = value.ToRitual();

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.Equal("Value was null", ritual.GetTear()!.Message);
    }

    [Fact]
    public void ToRitual_WithNullValueType_CustomMessage_CreatesTornRitualWithCustomMessage()
    {
        // Arrange
        decimal? value = null;

        // Act
        var ritual = value.ToRitual("Price cannot be null");

        // Assert
        Assert.True(ritual.IsTorn);
        Assert.Equal("Price cannot be null", ritual.GetTear()!.Message);
    }

    [Fact]
    public void ToRitual_WithReferenceType_Object_CreatesFlowingRitual()
    {
        // Arrange
        object? value = new { Name = "Test" };

        // Act
        var ritual = value.ToRitual();

        // Assert
        Assert.True(ritual.IsFlowing);
        Assert.NotNull(ritual.GetValue());
    }

    [Fact]
    public void ToRitual_WorksWithDifferentTypes()
    {
        // Arrange & Act
        var stringRitual = "hello".ToRitual();
        var intRitual = (42 as int?).ToRitual();
        var boolRitual = (true as bool?).ToRitual();

        // Assert
        Assert.True(stringRitual.IsFlowing);
        Assert.True(intRitual.IsFlowing);
        Assert.True(boolRitual.IsFlowing);
        Assert.Equal("hello", stringRitual.GetValue());
        Assert.Equal(42, intRitual.GetValue());
        Assert.True(boolRitual.GetValue());
    }
}
