using NogVita.Application.Foods;
using NogVita.Domain.Foods;

namespace NogVita.UnitTests.Application.Foods;

public class FoodResponseTests
{
    private static FoodResponse Create(FoodSource source, decimal? fiber) =>
        new(Guid.NewGuid(), "Alimento", null, null, null, source, "1", true, 100m, 1m, 20m, 1m, fiber, 5m);

    [Fact]
    public void Should_Cite_Taco()
    {
        Assert.Contains("TACO", Create(FoodSource.Taco, 1m).Attribution);
    }

    [Fact]
    public void Should_Cite_Open_Food_Facts_License()
    {
        Assert.Contains("ODbL", Create(FoodSource.OpenFoodFacts, 1m).Attribution);
    }

    [Fact]
    public void Should_Be_Incomplete_When_A_Main_Nutrient_Is_Missing()
    {
        Assert.False(Create(FoodSource.Taco, fiber: null).IsComplete);
        Assert.True(Create(FoodSource.Taco, fiber: 1m).IsComplete);
    }

    [Fact]
    public void Every_Source_Should_Have_Attribution()
    {
        foreach (var source in Enum.GetValues<FoodSource>())
        {
            Assert.False(string.IsNullOrWhiteSpace(FoodSourceInfo.Attribution(source)));
        }
    }
}