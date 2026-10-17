namespace NogVita.Application.NutritionPlans;

// OutsideRoutine: aviso, e não bloqueio. null quando o paciente não informou a rotina ou a refeição não tem horário.
public sealed record MealResponse(
    Guid Id,
    string Name,
    string? Notes,
    int Order,
    TimeOnly? SuggestedTime,
    IReadOnlyList<MealFoodResponse> Foods);
public sealed record NutritionPlanDetailResponse(NutritionPlanResponse Plan, IReadOnlyList<MealResponse> Meals);

