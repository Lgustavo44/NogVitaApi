using NogVita.Domain.Common;
using NogVita.Domain.Foods;

namespace NogVita.UnitTests.Domain.Foods;

public class NutrientsPer100gTests
{
    [Fact]
    public void Should_Accept_Real_Values()
    {
        var nutrients = new NutrientsPer100g(128.26m, 2.52m, 28.06m, 0.23m, 1.56m, 1.22m);

        Assert.Equal(128.26m, nutrients.EnergyKcal);
        Assert.True(nutrients.IsComplete);
    }

    [Fact]
    public void Should_Accept_Missing_Values_As_Null()
    {
        var nutrients = new NutrientsPer100g(480m, 6m, 70m, 20m, null, null);

        Assert.Null(nutrients.Fiber);
        Assert.False(nutrients.IsComplete);
    }

    [Fact]
    public void Should_Accept_Zero()
    {
        var nutrients = new NutrientsPer100g(0m, 0m, 0m, 0m, 0m, 0m);

        Assert.Equal(0m, nutrients.Fat);
    }

    [Fact]
    public void Should_Reject_Negative_Value()
    {
        Assert.Throws<DomainException>(() => new NutrientsPer100g(100m, -1m, 0m, 0m, 0m, 0m));
    }

    [Fact]
    public void Should_Reject_More_Than_100_Grams()
    {
        Assert.Throws<DomainException>(() => new NutrientsPer100g(400m, 150m, 0m, 0m, 0m, 0m));
    }

    [Fact]
    public void Should_Reject_Impossible_Energy()
    {
        Assert.Throws<DomainException>(() => new NutrientsPer100g(1500m, 10m, 10m, 10m, 0m, 0m));
    }

    [Fact]
    public void Should_Accept_Pure_Fat_Limits()
    {
        var oil = new NutrientsPer100g(884m, 0m, 0m, 100m, 0m, 0m);

        Assert.Equal(100m, oil.Fat);
    }
}