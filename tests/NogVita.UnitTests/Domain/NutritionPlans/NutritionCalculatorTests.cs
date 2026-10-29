using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Domain.NutritionPlans;

public class NutritionCalculatorTests
{
    private static readonly NutrientsPer100g Rice = new(128.26m, 2.52m, 28.06m, 0.23m, 1.56m, 1.22m);
    private static readonly NutrientsPer100g CocaZero = new(0m, 0m, 0m, 0m, null, 9m);

    [Fact]
    public void Should_Calculate_67_Grams_Of_Rice()
    {
        var portion = NutritionCalculator.Portion(Rice, 67m);

        Assert.Equal(85.9342m, portion.EnergyKcal);
        Assert.Equal(18.8002m, portion.Carbohydrate);
    }

    [Fact]
    public void Should_Keep_Unknown_As_Null()
    {
        var portion = NutritionCalculator.Portion(CocaZero, 350m);

        Assert.Equal(0m, portion.EnergyKcal);
        Assert.Null(portion.Fiber);
        Assert.Equal(31.5m, portion.SodiumMg);
    }

    [Fact]
    public void Meal_Total_Should_Mark_Only_The_Missing_Nutrient_As_Incomplete()
    {
        var totals = NutrientTotals.FromPortions(
        [
            NutritionCalculator.Portion(Rice, 100m),
            NutritionCalculator.Portion(CocaZero, 350m)
        ]);

        Assert.Equal(128.26m, totals.EnergyKcal.Value);
        Assert.True(totals.EnergyKcal.IsComplete);

        Assert.Equal(1.56m, totals.Fiber.Value);
        Assert.False(totals.Fiber.IsComplete);
    }

    [Fact]
    public void Empty_Meal_Should_Be_Zero_And_Complete()
    {
        var totals = NutrientTotals.FromPortions([]);

        Assert.Equal(0m, totals.EnergyKcal.Value);
        Assert.True(totals.EnergyKcal.IsComplete);
        Assert.Equal(NutrientTotals.Zero, totals);
    }

    [Fact]
    public void Day_Total_Should_Sum_Meals_And_Propagate_Incompleteness()
    {
        var lunch = NutrientTotals.FromPortions([NutritionCalculator.Portion(Rice, 150m)]);
        var snack = NutrientTotals.FromPortions([NutritionCalculator.Portion(CocaZero, 350m)]);

        var day = NutrientTotals.Combine([lunch, snack]);

        Assert.Equal(192.39m, day.EnergyKcal.Value);
        Assert.True(day.EnergyKcal.IsComplete);
        Assert.False(day.Fiber.IsComplete);
    }
}
