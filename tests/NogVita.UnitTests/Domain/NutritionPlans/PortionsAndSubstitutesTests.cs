using NogVita.Domain.Common;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Domain.NutritionPlans;

public class PortionsAndSubstitutesTests
{
    private static readonly DateTime Now = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    private static Food NewFood(int taco, string name) =>
        Food.FromTaco(taco, name, null, new NutrientsPer100g(100m, 10m, 10m, 1m, 1m, 1m));

    private static (NutritionPlan Plan, Meal Meal) PlanWithMeal()
    {
        var plan = new NutritionPlan(Guid.NewGuid(), Guid.NewGuid(), "Plano", null, new DateOnly(2026, 11, 1), null);
        return (plan, plan.AddMeal("Almoço", null));
    }

    // D-V2-34: a porção é como a quantidade foi escolhida; o cálculo usa os gramas.
    [Fact]
    public void Portion_Quantity_Should_Become_Grams_And_Keep_The_Measure()
    {
        var egg = NewFood(488, "Ovo, de galinha, inteiro, cozido");
        var unit = egg.AddPortion("unidade média", 45m, FoodPortionSource.Ibge)!;
        var (plan, meal) = PlanWithMeal();

        var item = plan.AddFoodToMeal(meal.Id, egg, FoodQuantity.OfPortion(unit, 2), Now);

        Assert.Equal(90m, item.QuantityInGrams);
        Assert.Equal("unidade média", item.PortionName);
        Assert.Equal(45m, item.PortionGrams);
        Assert.Equal(2m, item.PortionQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Portion_Quantity_Should_Be_Within_Limits(decimal quantity)
    {
        var portion = NewFood(1, "Arroz").AddPortion("colher de sopa cheia", 25m, FoodPortionSource.Ibge)!;

        Assert.Throws<DomainException>(() => FoodQuantity.OfPortion(portion, quantity));
    }

    [Fact]
    public void Portion_That_Exceeds_The_Gram_Limit_Should_Be_Refused()
    {
        var portion = NewFood(1, "Arroz").AddPortion("prato fundo cheio", 200m, FoodPortionSource.Ibge)!;

        Assert.Throws<DomainException>(() => FoodQuantity.OfPortion(portion, 11));
    }

    [Fact]
    public void A_Portion_With_The_Same_Name_Should_Not_Be_Added_Twice()
    {
        var rice = NewFood(1, "Arroz");

        Assert.NotNull(rice.AddPortion("Colher de sopa cheia", 25m, FoodPortionSource.Ibge));
        Assert.Null(rice.AddPortion("colher de sopa cheia", 30m, FoodPortionSource.Ibge));
        Assert.Single(rice.Portions);
    }

    // D-V2-36
    [Fact]
    public void Substitutes_Should_Point_To_A_Main_Item_And_Respect_The_Limit()
    {
        var (plan, meal) = PlanWithMeal();
        var chicken = plan.AddFoodToMeal(meal.Id, NewFood(1, "Frango"), 100m, Now);

        for (var taco = 2; taco <= 4; taco++)
        {
            var substitute = plan.AddFoodToMeal(meal.Id, NewFood(taco, $"Carne {taco}"), FoodQuantity.InGrams(100m), Now, chicken.Id);
            Assert.Equal(chicken.Id, substitute.SubstituteForId);
        }

        Assert.Throws<DomainException>(() =>
            plan.AddFoodToMeal(meal.Id, NewFood(5, "Peixe"), FoodQuantity.InGrams(100m), Now, chicken.Id));
    }

    [Fact]
    public void A_Substitute_Should_Not_Have_Substitutes()
    {
        var (plan, meal) = PlanWithMeal();
        var chicken = plan.AddFoodToMeal(meal.Id, NewFood(1, "Frango"), 100m, Now);
        var beef = plan.AddFoodToMeal(meal.Id, NewFood(2, "Patinho"), FoodQuantity.InGrams(100m), Now, chicken.Id);

        Assert.Throws<DomainException>(() =>
            plan.AddFoodToMeal(meal.Id, NewFood(3, "Ovo"), FoodQuantity.InGrams(100m), Now, beef.Id));
    }

    [Fact]
    public void Removing_The_Main_Item_Should_Remove_Its_Substitutes()
    {
        var (plan, meal) = PlanWithMeal();
        var chicken = plan.AddFoodToMeal(meal.Id, NewFood(1, "Frango"), 100m, Now);
        plan.AddFoodToMeal(meal.Id, NewFood(2, "Patinho"), FoodQuantity.InGrams(100m), Now, chicken.Id);
        var rice = plan.AddFoodToMeal(meal.Id, NewFood(3, "Arroz"), 100m, Now);

        plan.RemoveFoodFromMeal(meal.Id, chicken.Id);

        Assert.Equal([rice.Id], meal.Foods.Select(f => f.Id));
    }
}
