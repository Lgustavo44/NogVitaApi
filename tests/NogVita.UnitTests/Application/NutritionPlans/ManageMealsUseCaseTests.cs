using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FollowUps;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class ManageMealsUseCaseTests
{
    private readonly CareScenario _care = new();
    private readonly FakeNutritionPlanRepository _planRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly NutritionPlan _plan;

    public ManageMealsUseCaseTests()
    {

        _plan = new NutritionPlan(_care.Patient.Id, _care.Nutritionist.Id, "Plano", null, new DateOnly(2026, 10, 1), null);
        _planRepository.Add(_plan);
    }

    private ManageMealsUseCase CreateUseCase() =>
        new(new NutritionistPlanAccess(_care.CareAccess(), _planRepository), _unitOfWork);

    [Fact]
    public async Task Should_Add_Meal_To_Own_Plan()
    {
        var result = await CreateUseCase().AddAsync(
            _care.Nutritionist.Id, _plan.Id, new MealRequest("Café da manhã", null), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.Done, result.Result);
        var meal = Assert.Single(_plan.Meals);
        Assert.Equal(meal.Id, result.MealId);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Add_Meal_To_Finished_Plan()
    {
        _plan.Activate();
        _plan.Finish(new DateOnly(2026, 10, 20));

        var result = await CreateUseCase().AddAsync(
            _care.Nutritionist.Id, _plan.Id, new MealRequest("Ceia", null), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.InvalidState, result.Result);
        Assert.Empty(_plan.Meals);
    }

    [Fact]
    public async Task Should_Not_Remove_Meal_Of_Another_Plan()
    {
        var otherPlan = new NutritionPlan(_care.Patient.Id, _care.Nutritionist.Id, "Outro", null, new DateOnly(2026, 10, 1), null);
        var otherMeal = otherPlan.AddMeal("Almoço", null);
        _planRepository.Add(otherPlan);
        _plan.AddMeal("Café", null);

        var result = await CreateUseCase().RemoveAsync(
            _care.Nutritionist.Id, _plan.Id, otherMeal.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.NotFound, result);
        Assert.Single(otherPlan.Meals);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Reject_Incomplete_Order()
    {
        var breakfast = _plan.AddMeal("Café", null);
        _plan.AddMeal("Almoço", null);

        var result = await CreateUseCase().ReorderAsync(
            _care.Nutritionist.Id, _plan.Id, new ReorderMealsRequest([breakfast.Id]), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.InvalidOrder, result);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Reorder_Meals()
    {
        var breakfast = _plan.AddMeal("Café", null);
        var lunch = _plan.AddMeal("Almoço", null);

        var result = await CreateUseCase().ReorderAsync(
            _care.Nutritionist.Id, _plan.Id, new ReorderMealsRequest([lunch.Id, breakfast.Id]), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.Done, result);
        Assert.Equal(1, lunch.Order);
        Assert.Equal(2, breakfast.Order);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Report_Limit_Reached()
    {
        for (var i = 1; i <= NutritionPlan.MaxMeals; i++)
        {
            _plan.AddMeal($"Refeição {i}", null);
        }

        var result = await CreateUseCase().AddAsync(
            _care.Nutritionist.Id, _plan.Id, new MealRequest("Uma a mais", null), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.LimitReached, result.Result);
    }

    [Fact]
    public async Task Other_Nutritionist_Should_Not_See_The_Plan()
    {
        var result = await CreateUseCase().AddAsync(
            Guid.NewGuid(), _plan.Id, new MealRequest("Café", null), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.NotFound, result.Result);
        Assert.Empty(_plan.Meals);
    }

    [Fact]
    public async Task Should_Update_Meal_Without_Changing_Order()
    {
        _plan.AddMeal("Café", null);
        var lunch = _plan.AddMeal("Almoço", null);

        var result = await CreateUseCase().UpdateAsync(
            _care.Nutritionist.Id, _plan.Id, lunch.Id, new MealRequest("Almoço leve", "Sem fritura.", new TimeOnly(12, 30)),
            TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.Done, result);
        Assert.Equal("Almoço leve", lunch.Name);
        Assert.Equal(new TimeOnly(12, 30), lunch.SuggestedTime);
        Assert.Equal(2, lunch.Order);
    }

    [Fact]
    public async Task Should_Not_Update_Unknown_Meal()
    {
        var result = await CreateUseCase().UpdateAsync(
            _care.Nutritionist.Id, _plan.Id, Guid.NewGuid(), new MealRequest("Lanche", null), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.NotFound, result);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Update_Meal_Of_Finished_Plan()
    {
        var meal = _plan.AddMeal("Café", null);
        _plan.Activate();
        _plan.Finish(new DateOnly(2026, 10, 20));

        var result = await CreateUseCase().UpdateAsync(
            _care.Nutritionist.Id, _plan.Id, meal.Id, new MealRequest("Outro", null), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.InvalidState, result);
        Assert.Equal("Café", meal.Name);
    }

    [Fact]
    public async Task Should_Remove_Meal_And_Renumber_The_Others()
    {
        var breakfast = _plan.AddMeal("Café", null);
        var lunch = _plan.AddMeal("Almoço", null);
        var dinner = _plan.AddMeal("Jantar", null);

        var result = await CreateUseCase().RemoveAsync(_care.Nutritionist.Id, _plan.Id, lunch.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.Done, result);
        Assert.Equal([breakfast.Id, dinner.Id], _plan.Meals.OrderBy(m => m.Order).Select(m => m.Id));
        Assert.Equal(2, dinner.Order);
    }

    [Fact]
    public async Task Should_Not_Remove_Meal_Of_Finished_Plan()
    {
        var meal = _plan.AddMeal("Café", null);
        _plan.Activate();
        _plan.Finish(new DateOnly(2026, 10, 20));

        var result = await CreateUseCase().RemoveAsync(_care.Nutritionist.Id, _plan.Id, meal.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.InvalidState, result);
        Assert.Single(_plan.Meals);
    }
}
