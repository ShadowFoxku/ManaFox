using ManaFox.Core.Flow;
using ManaFox.Extensions.Flow;

namespace ManaFox.Tests.RitualTests;

public class RitualFlatteningTests
{
    [Fact]
    public void Flatten_WithNestedFlowingRituals_ReturnsFlowingRitual()
    {
        // Arrange
        var innerRitual = Ritual<int>.Flow(42);
        var nestedRitual = Ritual<Ritual<int>>.Flow(innerRitual);

        // Act
        var result = nestedRitual.Flatten();

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(42, result.GetValue());
    }

    [Fact]
    public void Flatten_WithNestedTornInnerRitual_ReturnsTornRitual()
    {
        // Arrange
        var innerRitual = Ritual<int>.Tear("Inner error");
        var nestedRitual = Ritual<Ritual<int>>.Flow(innerRitual);

        // Act
        var result = nestedRitual.Flatten();

        // Assert
        Assert.True(result.IsTorn);
        Assert.Equal("Inner error", result.GetTear()!.Message);
    }

    [Fact]
    public void Flatten_WithTornOuterRitual_ReturnsTornRitual()
    {
        // Arrange
        var nestedRitual = Ritual<Ritual<int>>.Tear("Outer error");

        // Act
        var result = nestedRitual.Flatten();

        // Assert
        Assert.True(result.IsTorn);
        Assert.Equal("Outer error", result.GetTear()!.Message);
    }

    [Fact]
    public void Flatten_WithComplexChaining_WorksCorrectly()
    {
        // Arrange
        var ritual = Ritual<int>.Flow(5)
            .Map(x => Ritual<int>.Flow(x * 2))
            .Flatten()
            .Map(x => Ritual<int>.Flow(x + 3))
            .Flatten();

        // Act
        var result = ritual;

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(13, result.GetValue());
    }

    [Fact]
    public async Task FlattenAsync_WithNestedFlowingRituals_ReturnsFlowingRitual()
    {
        // Arrange
        var innerRitual = Ritual<int>.Flow(42);
        var nestedRitualTask = Task.FromResult(Ritual<Ritual<int>>.Flow(innerRitual));

        // Act
        var result = await nestedRitualTask.FlattenAsync();

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(42, result.GetValue());
    }

    [Fact]
    public async Task FlattenAsync_WithNestedTornInnerRitual_ReturnsTornRitual()
    {
        // Arrange
        var innerRitual = Ritual<string>.Tear("Inner async error");
        var nestedRitualTask = Task.FromResult(Ritual<Ritual<string>>.Flow(innerRitual));

        // Act
        var result = await nestedRitualTask.FlattenAsync();

        // Assert
        Assert.True(result.IsTorn);
        Assert.Equal("Inner async error", result.GetTear()!.Message);
    }

    [Fact]
    public async Task FlattenAsync_WithAsyncChaining_WorksCorrectly()
    {
        // Arrange
        var innerRitual = Ritual<int>.Flow(5);
        var nestedRitual = Ritual<Ritual<int>>.Flow(innerRitual);
        var ritualTask = Task.FromResult(nestedRitual);

        // Act
        var result = await ritualTask.FlattenAsync();

        // Assert
        Assert.True(result.IsFlowing);
        Assert.Equal(5, result.GetValue());
    }
}
