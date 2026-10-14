using NogVita.Domain.Common;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Domain.NutritionPlans;

public class NutritionPlanMealsTests
{
    private static NutritionPlan CreatePlan() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Plano", null, new DateOnly(2026, 10, 1), null);

    private static string[] NamesInOrder(NutritionPlan plan) =>
        plan.Meals.OrderBy(m => m.Order).Select(m => m.Name).ToArray();

    [Fact]
    public void Should_Add_Meals_In_Sequence()
    {
        var plan = CreatePlan();

        var breakfast = plan.AddMeal("  Café da manhã  ", "  Em jejum, beber água antes.  ");
        plan.AddMeal("Almoço", null);

        Assert.Equal("Café da manhã", breakfast.Name);
        Assert.Equal("Em jejum, beber água antes.", breakfast.Notes);
        Assert.Equal(plan.Id, breakfast.NutritionPlanId);
        Assert.Equal(new[] { 1, 2 }, plan.Meals.Select(m => m.Order).OrderBy(o => o));
    }

    [Fact]
    public void Should_Renumber_After_Removing_From_The_Middle()
    {
        var plan = CreatePlan();
        plan.AddMeal("Café", null);
        var lunch = plan.AddMeal("Almoço", null);
        plan.AddMeal("Jantar", null);

        plan.RemoveMeal(lunch.Id);

        Assert.Equal(new[] { "Café", "Jantar" }, NamesInOrder(plan));
        Assert.Equal(new[] { 1, 2 }, plan.Meals.Select(m => m.Order).OrderBy(o => o));
    }

    [Fact]
    public void Should_Reorder_With_Full_List()
    {
        var plan = CreatePlan();
        var breakfast = plan.AddMeal("Café", null);
        var lunch = plan.AddMeal("Almoço", null);
        var dinner = plan.AddMeal("Jantar", null);

        plan.ReorderMeals([dinner.Id, breakfast.Id, lunch.Id]);

        Assert.Equal(new[] { "Jantar", "Café", "Almoço" }, NamesInOrder(plan));
    }

    [Fact]
    public void Should_Reject_Reorder_With_Missing_Meal()
    {
        var plan = CreatePlan();
        var breakfast = plan.AddMeal("Café", null);
        plan.AddMeal("Almoço", null);

        Assert.Throws<DomainException>(() => plan.ReorderMeals([breakfast.Id]));
    }

    [Fact]
    public void Should_Reject_Reorder_With_Repeated_Meal()
    {
        var plan = CreatePlan();
        var breakfast = plan.AddMeal("Café", null);
        plan.AddMeal("Almoço", null);

        Assert.Throws<DomainException>(() => plan.ReorderMeals([breakfast.Id, breakfast.Id]));
    }

    [Fact]
    public void Should_Reject_Reorder_With_Meal_From_Another_Plan()
    {
        var plan = CreatePlan();
        var breakfast = plan.AddMeal("Café", null);
        plan.AddMeal("Almoço", null);

        Assert.Throws<DomainException>(() => plan.ReorderMeals([breakfast.Id, Guid.NewGuid()]));
        Assert.Equal(new[] { "Café", "Almoço" }, NamesInOrder(plan));
    }

    [Fact]
    public void Should_Not_Change_Meals_Of_Finished_Plan()
    {
        var plan = CreatePlan();
        var breakfast = plan.AddMeal("Café", null);
        plan.Activate();
        plan.Finish(new DateOnly(2026, 10, 20));

        Assert.Throws<DomainException>(() => plan.AddMeal("Ceia", null));
        Assert.Throws<DomainException>(() => plan.UpdateMeal(breakfast.Id, "Outro nome", null));
        Assert.Throws<DomainException>(() => plan.RemoveMeal(breakfast.Id));
        Assert.Throws<DomainException>(() => plan.ReorderMeals([breakfast.Id]));
    }

    [Fact]
    public void Should_Limit_Number_Of_Meals()
    {
        var plan = CreatePlan();

        for (var i = 1; i <= NutritionPlan.MaxMeals; i++)
        {
            plan.AddMeal($"Refeição {i}", null);
        }

        Assert.Throws<DomainException>(() => plan.AddMeal("Uma a mais", null));
    }

    [Fact]
    public void Should_Reject_Empty_Meal_Name()
    {
        var plan = CreatePlan();

        Assert.Throws<DomainException>(() => plan.AddMeal("   ", null));
        Assert.Empty(plan.Meals);
    }

    [Fact]
    public void Should_Keep_Suggested_Time_Without_Changing_Order()
    {
        var plan = CreatePlan();
        var breakfast = plan.AddMeal("Café da manhã", null, new TimeOnly(7, 0));
        // Fora de ordem cronológica de propósito: o horário não reordena as refeições.
        var lateSnack = plan.AddMeal("Ceia", null, new TimeOnly(3, 0));

        Assert.Equal(new TimeOnly(7, 0), breakfast.SuggestedTime);
        Assert.Equal(new TimeOnly(3, 0), lateSnack.SuggestedTime);
        Assert.Equal(new[] { "Café da manhã", "Ceia" }, NamesInOrder(plan));
    }

    [Fact]
    public void Should_Update_And_Clear_Suggested_Time()
    {
        var plan = CreatePlan();
        var lunch = plan.AddMeal("Almoço", null, new TimeOnly(12, 0));

        plan.UpdateMeal(lunch.Id, "Almoço", null, new TimeOnly(13, 30));
        Assert.Equal(new TimeOnly(13, 30), lunch.SuggestedTime);

        plan.UpdateMeal(lunch.Id, "Almoço", null, null);
        Assert.Null(lunch.SuggestedTime);
    }
}