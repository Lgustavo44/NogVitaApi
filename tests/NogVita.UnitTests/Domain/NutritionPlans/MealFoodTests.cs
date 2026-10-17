using NogVita.Domain.Common;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Domain.NutritionPlans;

public class MealFoodTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    private static Food CreateRice() =>
        Food.FromTaco(3, "Arroz, tipo 1, cozido", "Cereais e derivados", new NutrientsPer100g(128.26m, 2.52m, 28.06m, 0.23m, 1.56m, 1.22m));

    private static (NutritionPlan Plan, Meal Meal) CreatePlanWithMeal()
    {
        var plan = new NutritionPlan(Guid.NewGuid(), Guid.NewGuid(), "Plano", null, new DateOnly(2026, 10, 1), null);
        var meal = plan.AddMeal("Almoço", null);
        return (plan, meal);
    }

    [Fact]
    public void Should_Add_Food_With_Snapshot()
    {
        var (plan, meal) = CreatePlanWithMeal();
        var rice = CreateRice();

        var item = plan.AddFoodToMeal(meal.Id, rice, 150.456m, Now);

        Assert.Equal(rice.Id, item.FoodId);
        Assert.Equal(150.46m, item.QuantityInGrams);
        Assert.Equal("Arroz, tipo 1, cozido", item.Snapshot.Name);
        Assert.Equal(FoodSource.Taco, item.Snapshot.Source);
        Assert.Equal("3", item.Snapshot.SourceReference);
        Assert.Equal(128.26m, item.Snapshot.Nutrients.EnergyKcal);
        Assert.Equal(Now, item.Snapshot.TakenAtUtc);
    }

    [Fact]
    public void Snapshot_Should_Not_Change_When_Food_Is_Deactivated()
    {
        var (plan, meal) = CreatePlanWithMeal();
        var rice = CreateRice();
        var item = plan.AddFoodToMeal(meal.Id, rice, 100m, Now);

        rice.Deactivate();

        Assert.Equal("Arroz, tipo 1, cozido", item.Snapshot.Name);
        Assert.Equal(128.26m, item.Snapshot.Nutrients.EnergyKcal);
    }

    [Fact]
    public void Should_Reject_Inactive_Food()
    {
        var (plan, meal) = CreatePlanWithMeal();
        var rice = CreateRice();
        rice.Deactivate();

        Assert.Throws<DomainException>(() => plan.AddFoodToMeal(meal.Id, rice, 100m, Now));
        Assert.Empty(meal.Foods);
    }

    [Fact]
    public void Should_Reject_Same_Food_Twice()
    {
        var (plan, meal) = CreatePlanWithMeal();
        var rice = CreateRice();
        plan.AddFoodToMeal(meal.Id, rice, 100m, Now);

        Assert.Throws<DomainException>(() => plan.AddFoodToMeal(meal.Id, rice, 50m, Now));
        Assert.Single(meal.Foods);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(2000.01)]
    public void Should_Reject_Invalid_Quantity(double grams)
    {
        var (plan, meal) = CreatePlanWithMeal();

        Assert.Throws<DomainException>(() => plan.AddFoodToMeal(meal.Id, CreateRice(), (decimal)grams, Now));
    }

    [Fact]
    public void Updating_Quantity_Should_Keep_Snapshot()
    {
        var (plan, meal) = CreatePlanWithMeal();
        var item = plan.AddFoodToMeal(meal.Id, CreateRice(), 100m, Now);
        var originalSnapshot = item.Snapshot;

        plan.UpdateMealFoodQuantity(meal.Id, item.Id, 180m);

        Assert.Equal(180m, item.QuantityInGrams);
        Assert.Same(originalSnapshot, item.Snapshot);
    }

    [Fact]
    public void Should_Remove_Food()
    {
        var (plan, meal) = CreatePlanWithMeal();
        var item = plan.AddFoodToMeal(meal.Id, CreateRice(), 100m, Now);

        plan.RemoveFoodFromMeal(meal.Id, item.Id);

        Assert.Empty(meal.Foods);
    }

    [Fact]
    public void Should_Not_Change_Foods_Of_Finished_Plan()
    {
        var (plan, meal) = CreatePlanWithMeal();
        var item = plan.AddFoodToMeal(meal.Id, CreateRice(), 100m, Now);
        plan.Activate();
        plan.Finish(new DateOnly(2026, 10, 20));

        var bean = Food.FromTaco(561, "Feijão, carioca, cozido", null, new NutrientsPer100g(76m, 4.8m, 13.6m, 0.5m, 8.5m, 2m));

        Assert.Throws<DomainException>(() => plan.AddFoodToMeal(meal.Id, bean, 80m, Now));
        Assert.Throws<DomainException>(() => plan.UpdateMealFoodQuantity(meal.Id, item.Id, 200m));
        Assert.Throws<DomainException>(() => plan.RemoveFoodFromMeal(meal.Id, item.Id));
    }

    [Fact]
    public void Should_Limit_Foods_Per_Meal()
    {
        var (plan, meal) = CreatePlanWithMeal();

        for (var i = 1; i <= Meal.MaxFoods; i++)
        {
            var food = Food.FromTaco(i, $"Alimento {i}", null, new NutrientsPer100g(100m, 1m, 1m, 1m, 1m, 1m));
            plan.AddFoodToMeal(meal.Id, food, 10m, Now);
        }

        var extra = Food.FromTaco(999, "Um a mais", null, new NutrientsPer100g(100m, 1m, 1m, 1m, 1m, 1m));

        Assert.Throws<DomainException>(() => plan.AddFoodToMeal(meal.Id, extra, 10m, Now));
    }
}