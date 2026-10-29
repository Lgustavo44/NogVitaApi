using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class ManageMealFoodsUseCaseTests
{
    private readonly CareScenario _care = new();
    private readonly FakeNutritionPlanRepository _planRepository = new();
    private readonly FakeFoodRepository _foodRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly NutritionPlan _plan;
    private readonly Meal _meal;
    private readonly Food _rice;

    public ManageMealFoodsUseCaseTests()
    {

        _plan = new NutritionPlan(_care.Patient.Id, _care.Nutritionist.Id, "Plano", null, new DateOnly(2026, 10, 1), null);
        _meal = _plan.AddMeal("Almoço", null);
        _planRepository.Add(_plan);

        _rice = Food.FromTaco(3, "Arroz, tipo 1, cozido", null, new NutrientsPer100g(128.26m, 2.52m, 28.06m, 0.23m, 1.56m, 1.22m));
        _foodRepository.Add(_rice);
    }

    private ManageMealFoodsUseCase CreateUseCase() =>
        new(
            new NutritionistPlanAccess(_care.CareAccess(), _planRepository),
            _foodRepository,
            _unitOfWork,
            TimeProvider.System);

    [Fact]
    public async Task Should_Add_Active_Food()
    {
        var result = await CreateUseCase().AddAsync(
            _care.Nutritionist.Id, _plan.Id, _meal.Id, new AddMealFoodRequest(_rice.Id, 150m), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealFoodResult.Done, result.Result);
        var item = Assert.Single(_meal.Foods);
        Assert.Equal(item.Id, result.MealFoodId);
        Assert.NotEqual(_rice.Id, result.MealFoodId);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Reject_Inactive_Food()
    {
        _rice.Deactivate();

        var result = await CreateUseCase().AddAsync(
            _care.Nutritionist.Id, _plan.Id, _meal.Id, new AddMealFoodRequest(_rice.Id, 150m), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealFoodResult.FoodNotFound, result.Result);
        Assert.Empty(_meal.Foods);
    }

    [Fact]
    public async Task Should_Reject_Same_Food_Twice()
    {
        var useCase = CreateUseCase();
        await useCase.AddAsync(_care.Nutritionist.Id, _plan.Id, _meal.Id, new AddMealFoodRequest(_rice.Id, 150m), TestContext.Current.CancellationToken);

        var second = await useCase.AddAsync(
            _care.Nutritionist.Id, _plan.Id, _meal.Id, new AddMealFoodRequest(_rice.Id, 50m), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealFoodResult.AlreadyInMeal, second.Result);
        Assert.Single(_meal.Foods);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Add_To_Meal_Of_Another_Plan()
    {
        var otherPlan = new NutritionPlan(_plan.PatientId, _care.Nutritionist.Id, "Outro", null, new DateOnly(2026, 10, 1), null);
        var otherMeal = otherPlan.AddMeal("Jantar", null);
        _planRepository.Add(otherPlan);

        var result = await CreateUseCase().AddAsync(
            _care.Nutritionist.Id, _plan.Id, otherMeal.Id, new AddMealFoodRequest(_rice.Id, 150m), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealFoodResult.NotFound, result.Result);
        Assert.Empty(otherMeal.Foods);
    }

    [Fact]
    public async Task Should_Update_Quantity_And_Keep_Snapshot()
    {
        var item = _plan.AddFoodToMeal(_meal.Id, _rice, 100m, DateTime.UtcNow);
        var snapshot = item.Snapshot;

        var result = await CreateUseCase().UpdateQuantityAsync(
            _care.Nutritionist.Id, _plan.Id, _meal.Id, item.Id, new UpdateMealFoodRequest(200m), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealFoodResult.Done, result);
        Assert.Equal(200m, item.QuantityInGrams);
        Assert.Same(snapshot, item.Snapshot);
    }

    [Fact]
    public async Task Should_Remove_Item()
    {
        var item = _plan.AddFoodToMeal(_meal.Id, _rice, 100m, DateTime.UtcNow);

        var result = await CreateUseCase().RemoveAsync(
            _care.Nutritionist.Id, _plan.Id, _meal.Id, item.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealFoodResult.Done, result);
        Assert.Empty(_meal.Foods);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Change_Finished_Plan()
    {
        var item = _plan.AddFoodToMeal(_meal.Id, _rice, 100m, DateTime.UtcNow);
        _plan.Activate();
        _plan.Finish(new DateOnly(2026, 10, 20));

        var result = await CreateUseCase().RemoveAsync(
            _care.Nutritionist.Id, _plan.Id, _meal.Id, item.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealFoodResult.InvalidState, result);
        Assert.Single(_meal.Foods);
    }

    [Fact]
    public async Task Should_Report_Limit_Reached()
    {
        for (var i = 0; i < Meal.MaxFoods; i++)
        {
            var food = Food.FromTaco(100 + i, $"Alimento {i}", null, new NutrientsPer100g(100m, 1m, 1m, 1m, 1m, 1m));
            _plan.AddFoodToMeal(_meal.Id, food, 10m, DateTime.UtcNow);
        }

        var result = await CreateUseCase().AddAsync(
            _care.Nutritionist.Id, _plan.Id, _meal.Id, new AddMealFoodRequest(_rice.Id, 100m), TestContext.Current.CancellationToken);

        Assert.Equal(ManageMealFoodResult.LimitReached, result.Result);
        Assert.Equal(Meal.MaxFoods, _meal.Foods.Count);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}
