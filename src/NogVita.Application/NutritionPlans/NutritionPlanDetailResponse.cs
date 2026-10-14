namespace NogVita.Application.NutritionPlans;

public sealed record MealResponse(Guid Id, string Name, string? Notes, TimeOnly? SuggestedTime, int Order);

public sealed record NutritionPlanDetailResponse(NutritionPlanResponse Plan, IReadOnlyList<MealResponse> Meals);