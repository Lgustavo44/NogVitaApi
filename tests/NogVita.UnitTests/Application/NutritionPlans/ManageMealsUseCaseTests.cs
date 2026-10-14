using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FollowUps;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class ManageMealsUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FakeNutritionPlanRepository _planRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly User _nutritionist;
    private readonly NutritionPlan _plan;

    public ManageMealsUseCaseTests()
    {
        _nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        _nutritionist.SetPasswordHash("hash");
        _nutritionist.ConfirmEmail(DateTime.UtcNow);
        _nutritionist.CreateNutritionistProfile(1, "123456");
        _nutritionist.NutritionistProfile!.Activate();
        _userRepository.Add(_nutritionist);

        _relationshipRepository.Add(new NutritionistRequest(_patientId, _nutritionist.Id, null).Accept(DateTime.UtcNow));

        _plan = new NutritionPlan(_patientId, _nutritionist.Id, "Plano", null, new DateOnly(2026, 10, 1), null);
        _planRepository.Add(_plan);
    }

    private ManageMealsUseCase CreateUseCase() =>
        new(new NutritionistPlanAccess(new CareAccessService(_userRepository, _relationshipRepository), _planRepository), _unitOfWork);

    [Fact]
    public async Task Should_Add_Meal_To_Own_Plan()
    {
        var result = await CreateUseCase().AddAsync(
            _nutritionist.Id, _plan.Id, new MealRequest("Café da manhã", null), TestContext.Current.CancellationToken);

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
            _nutritionist.Id, _plan.Id, new MealRequest("Ceia", null), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.InvalidState, result.Result);
        Assert.Empty(_plan.Meals);
    }

    [Fact]
    public async Task Should_Not_Remove_Meal_Of_Another_Plan()
    {
        var otherPlan = new NutritionPlan(_patientId, _nutritionist.Id, "Outro", null, new DateOnly(2026, 10, 1), null);
        var otherMeal = otherPlan.AddMeal("Almoço", null);
        _planRepository.Add(otherPlan);
        _plan.AddMeal("Café", null);

        var result = await CreateUseCase().RemoveAsync(
            _nutritionist.Id, _plan.Id, otherMeal.Id, TestContext.Current.CancellationToken);

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
            _nutritionist.Id, _plan.Id, new ReorderMealsRequest([breakfast.Id]), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealResult.InvalidOrder, result);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Reorder_Meals()
    {
        var breakfast = _plan.AddMeal("Café", null);
        var lunch = _plan.AddMeal("Almoço", null);

        var result = await CreateUseCase().ReorderAsync(
            _nutritionist.Id, _plan.Id, new ReorderMealsRequest([lunch.Id, breakfast.Id]), TestContext.Current.CancellationToken);

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
            _nutritionist.Id, _plan.Id, new MealRequest("Uma a mais", null), TestContext.Current.CancellationToken);

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
}