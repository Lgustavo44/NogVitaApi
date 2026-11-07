using NogVita.Application.Foods;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FoodDiary;
using NogVita.Domain.Foods;

namespace NogVita.Application.FoodDiary;

public sealed record ExtraFoodResponse(
    Guid Id,
    Guid FoodId,
    string Name,
    string? Brand,
    FoodSource Source,
    decimal QuantityInGrams,
    MeasureResponse? Measure,
    NutrientAmountsResponse Amounts)
{
    public string Attribution => FoodSourceInfo.Attribution(Source);
}

// Uma refeição do dia: as do plano ativo ainda sem resposta (Adherence nulo) e as já respondidas, com o retrato
// do planejado no momento do check-in.
public sealed record DiaryMealResponse(
    Guid MealId,
    string Name,
    int Order,
    TimeOnly? SuggestedTime,
    NutrientTotalsResponse Planned,
    MealAdherence? Adherence,
    string? Notes,
    IReadOnlyList<ExtraFoodResponse> Extras);

// O consumido é uma estimativa: as refeições seguidas (pelo planejado) mais os extras. ConsumedIsComplete fica false
// quando há refeição "em parte" ou ainda sem resposta.
public sealed record DiaryDayResponse(
    DateOnly Date,
    bool HasActivePlan,
    IReadOnlyList<DiaryMealResponse> Meals,
    IReadOnlyList<ExtraFoodResponse> ExtrasOutsideMeals,
    NutrientTotalsResponse Planned,
    NutrientTotalsResponse Consumed,
    bool ConsumedIsComplete,
    bool MacrosVisible);

public sealed record AdherenceResponse(int Followed, int Partial, int Skipped)
{
    public int Answered => Followed + Partial + Skipped;

    // A parte das refeições respondidas que foi seguida, de 0 a 100. Sem resposta: null.
    public decimal? FollowedPercent => Answered == 0 ? null : Math.Round(100m * Followed / Answered, 1);
}

public sealed record DiaryRangeResponse(DateOnly From, DateOnly To, IReadOnlyList<DiaryDayResponse> Days, AdherenceResponse Adherence);

public sealed record WeeklyCheckInResponse(
    Guid Id,
    DateOnly WeekStart,
    int Hunger,
    int Energy,
    int Sleep,
    string? Difficulties,
    string? Comment,
    string? Feedback,
    DateTime? FeedbackAtUtc,
    DateTime UpdatedAt)
{
    public static WeeklyCheckInResponse From(WeeklyCheckIn checkIn) => new(
        checkIn.Id, checkIn.WeekStart, checkIn.Hunger, checkIn.Energy, checkIn.Sleep, checkIn.Difficulties, checkIn.Comment,
        checkIn.Feedback, checkIn.FeedbackAtUtc, checkIn.UpdatedAt ?? checkIn.CreatedAt);
}

public sealed record AwaitingFeedbackResponse(Guid Id, Guid PatientId, string PatientName, DateOnly WeekStart, DateTime UpdatedAt);
