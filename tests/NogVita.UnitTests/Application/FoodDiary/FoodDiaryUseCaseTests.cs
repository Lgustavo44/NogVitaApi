using NogVita.Application.FoodDiary;
using NogVita.Domain.Common;
using NogVita.Domain.FoodDiary;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.FoodDiary;

public class FoodDiaryUseCaseTests
{
    private static readonly DateOnly Today = new(2026, 11, 7);
    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
    private readonly CareScenario _care = new();
    private readonly FakeNutritionPlanRepository _plans = new();
    private readonly FakeFoodRepository _foods = new();
    private readonly FakeFoodDiaryRepository _diary = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly NutritionPlan _plan;
    private readonly Meal _lunch;
    private readonly Food _rice;

    public FoodDiaryUseCaseTests()
    {
        _rice = Food.FromTaco(3, "Arroz, tipo 1, cozido", null, new NutrientsPer100g(128m, 2.5m, 28m, 0.2m, 1.6m, 1.2m));
        var beans = Food.FromTaco(561, "Feijão, carioca, cozido", null, new NutrientsPer100g(76m, 4.8m, 13.6m, 0.5m, 8.5m, 2m));
        _foods.Add(_rice);

        _plan = new NutritionPlan(_care.Patient.Id, _care.Nutritionist.Id, "Plano", null, new DateOnly(2026, 11, 1), null);
        _lunch = _plan.AddMeal("Almoço", null);
        var main = _plan.AddFoodToMeal(_lunch.Id, _rice, 100m, DateTime.UtcNow);
        _plan.AddFoodToMeal(_lunch.Id, beans, FoodQuantity.InGrams(100m), DateTime.UtcNow, main.Id);
        _plan.Activate();
        _plans.Add(_plan);
    }

    private FoodDiaryUseCase UseCase() => new(_care.Users, _diary, _plans, _foods, _unitOfWork, _clock);

    private FoodDiaryReader Reader() => new(_diary, _plans, _care.CareAccess(), _clock);

    [Fact]
    public async Task Check_In_Should_Keep_The_Planned_Of_The_Main_Items_Only()
    {
        var outcome = await UseCase().CheckInMealAsync(_care.Patient.Id, Today, _lunch.Id, new(MealAdherence.Followed, null), _ct);

        Assert.Equal(FoodDiaryResult.Done, outcome.Result);
        var checkIn = Assert.Single(Assert.Single(_diary.Days).MealCheckIns);
        Assert.Equal(128m, checkIn.Planned.EnergyKcal); // o arroz, sem a alternativa (feijão)
    }

    // D-V2-22
    [Fact]
    public async Task Writes_Should_Require_The_Health_Data_Consent()
    {
        var oldPatient = TestUsers.Patient(email: "antigo@email.com", healthDataConsent: false).AddTo(_care.Users);

        await Assert.ThrowsAsync<DomainException>(() =>
            UseCase().AddExtraFoodAsync(oldPatient.Id, Today, new(_rice.Id, null, 50m), _ct));
    }

    [Fact]
    public async Task Check_In_Should_Refuse_A_Meal_Outside_The_Active_Plan()
    {
        var outcome = await UseCase().CheckInMealAsync(_care.Patient.Id, Today, Guid.NewGuid(), new(MealAdherence.Skipped, null), _ct);

        Assert.Equal(FoodDiaryResult.MealNotFound, outcome.Result);
        Assert.Empty(_diary.Days);
    }

    [Fact]
    public async Task Day_Should_Estimate_Consumed_From_Followed_Meals_And_Extras()
    {
        await UseCase().CheckInMealAsync(_care.Patient.Id, Today, _lunch.Id, new(MealAdherence.Followed, null), _ct);
        await UseCase().AddExtraFoodAsync(_care.Patient.Id, Today, new(_rice.Id, null, 50m), _ct);

        var day = await Reader().GetDayForPatientAsync(_care.Patient.Id, Today, _ct);

        Assert.Equal(192m, day.Consumed.EnergyKcal.Value);
        Assert.True(day.ConsumedIsComplete);
        Assert.Single(day.ExtrasOutsideMeals);
    }

    // D-V2-25: o paciente sem macros no plano não os vê no diário.
    [Fact]
    public async Task Patient_Should_Not_See_Macros_When_The_Plan_Hides_Them()
    {
        var day = await Reader().GetDayForPatientAsync(_care.Patient.Id, Today, _ct);

        Assert.False(day.MacrosVisible);
        Assert.Null(day.Planned.Protein);
        Assert.Equal(128m, day.Planned.EnergyKcal.Value);
        Assert.Null(Assert.Single(day.Meals).Adherence);
    }

    // D-V2-06
    [Fact]
    public async Task Nutritionist_Without_The_Follow_Up_Should_Not_Read_The_Diary()
    {
        var stranger = _care.AnotherNutritionist();

        Assert.Null(await Reader().GetRangeForNutritionistAsync(stranger.Id, _care.Patient.Id, new(), _ct));
        Assert.NotNull(await Reader().GetRangeForNutritionistAsync(_care.Nutritionist.Id, _care.Patient.Id, new(), _ct));
    }

    [Fact]
    public async Task Only_The_Following_Nutritionist_Should_Give_Feedback()
    {
        var weekly = new WeeklyCheckInUseCase(_care.Users, _diary, _care.Relationships, _care.CareAccess(), _unitOfWork, _clock);
        var saved = await weekly.SaveForPatientAsync(_care.Patient.Id, new DateOnly(2026, 11, 2), new(3, 4, 3, null, "Semana boa"), _ct);

        var stranger = await weekly.GiveFeedbackAsync(_care.AnotherNutritionist().Id, saved.CheckIn!.Id, new("Oi"), _ct);
        var own = await weekly.GiveFeedbackAsync(_care.Nutritionist.Id, saved.CheckIn.Id, new("Continue assim!"), _ct);

        Assert.Equal(WeeklyCheckInResult.NotFound, stranger.Result);
        Assert.Equal("Continue assim!", own.CheckIn!.Feedback);
        Assert.Empty(await weekly.ListAwaitingFeedbackAsync(_care.Nutritionist.Id, _ct));
    }
}
