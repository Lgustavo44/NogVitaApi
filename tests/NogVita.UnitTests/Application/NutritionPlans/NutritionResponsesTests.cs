using NogVita.Application.NutritionPlans;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class NutritionResponsesTests
{
    private static MealFoodResponse Item(decimal grams, decimal kcal, decimal protein, decimal? fiber) =>
        new(Guid.NewGuid(), Guid.NewGuid(), grams, "Alimento", null, FoodSource.Taco, "1", DateTime.UtcNow,
            kcal, protein, 10m, 1m, fiber, 5m);

    private static MealResponse Meal(params MealFoodResponse[] foods) =>
        new(Guid.NewGuid(), "Refeição", null, 1, null, foods);

    private static NutritionPlanResponse Plan(decimal? targetKcal = null, decimal? targetProtein = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Paciente", Guid.NewGuid(), "Nutri", "Plano", null,
            new DateOnly(2026, 10, 1), null, NutritionPlanStatus.Active, DateTime.UtcNow,
            targetKcal, targetProtein, null, null);

    [Fact]
    public void Portion_Should_Be_Rounded_To_One_Decimal()
    {
        var rice = Item(67m, 128.26m, 2.52m, 1.56m);

        Assert.Equal(85.9m, rice.Portion.EnergyKcal);
    }

    [Fact]
    public void Rounding_Should_Go_Away_From_Zero_At_Midpoint()
    {
        Assert.Equal(2.3m, NutritionRounding.Round(2.25m));
        Assert.Equal(2.4m, NutritionRounding.Round(2.35m));
    }

    [Fact]
    public void Meal_Total_Should_Sum_Unrounded_Portions()
    {
        var meal = Meal(Item(33m, 100.15m, 1m, 1m), Item(33m, 100.15m, 1m, 1m));

        Assert.Equal(33.0m, meal.Foods.First().Portion.EnergyKcal);
        Assert.Equal(66.1m, meal.Totals.EnergyKcal.Value);
    }

    [Fact]
    public void Day_Total_Should_Combine_Meals_And_Flag_Missing_Fiber()
    {
        var plan = new NutritionPlanDetailResponse(
            Plan(),
            [Meal(Item(100m, 200m, 10m, 2m)), Meal(Item(100m, 100m, 5m, null))]);

        Assert.Equal(300m, plan.DailyTotals.EnergyKcal.Value);
        Assert.True(plan.DailyTotals.EnergyKcal.IsComplete);
        Assert.Equal(2m, plan.DailyTotals.Fiber.Value);
        Assert.False(plan.DailyTotals.Fiber.IsComplete);
    }

    [Fact]
    public void Should_Compare_With_Target()
    {
        var plan = new NutritionPlanDetailResponse(
            Plan(targetKcal: 2000m, targetProtein: 150m),
            // 250 g de um alimento com 740 kcal e 12 g de proteína por 100 g: 1850 kcal e 30 g na porção.
            // (1850 kcal por 100 g seria recusado: o domínio não aceita mais de 900 kcal em 100 g.)
            [Meal(Item(250m, 740m, 12m, 1m))]);

        Assert.NotNull(plan.Target);
        Assert.Equal(-150m, plan.Target.EnergyKcal.Difference);
        Assert.Equal(-120m, plan.Target.Protein!.Difference);
        Assert.Null(plan.Target.Carbohydrate);
    }

    [Fact]
    public void Should_Have_No_Comparison_Without_Target()
    {
        var plan = new NutritionPlanDetailResponse(Plan(), [Meal(Item(100m, 100m, 1m, 1m))]);

        Assert.Null(plan.Target);
    }
}