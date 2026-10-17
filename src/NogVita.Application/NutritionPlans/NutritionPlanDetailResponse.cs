using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

// OutsideRoutine: aviso, e não bloqueio. null quando o paciente não informou a rotina ou a refeição não tem horário.
public sealed record MealResponse(
    Guid Id,
    string Name,
    string? Notes,
    int Order,
    TimeOnly? SuggestedTime,
    IReadOnlyList<MealFoodResponse> Foods)
{
    internal NutrientTotals RawTotals => NutrientTotals.FromPortions(Foods.Select(f => f.RawPortion));

    public NutrientTotalsResponse Totals => NutrientTotalsResponse.From(RawTotals);
}

public sealed record NutritionPlanDetailResponse(NutritionPlanResponse Plan, IReadOnlyList<MealResponse> Meals)
{
    private NutrientTotals RawDailyTotals => NutrientTotals.Combine(Meals.Select(m => m.RawTotals));

    public NutrientTotalsResponse DailyTotals => NutrientTotalsResponse.From(RawDailyTotals);

    // null quando o plano não tem meta. A meta de energia é obrigatória numa meta; as de macronutrientes, opcionais.
    public TargetComparisonResponse? Target
    {
        get
        {
            if (Plan.TargetEnergyKcal is null)
            {
                return null;
            }

            var daily = RawDailyTotals;

            return new TargetComparisonResponse(
                EnergyKcal: NutrientComparisonResponse.From(Plan.TargetEnergyKcal.Value, daily.EnergyKcal),
                Protein: Compare(Plan.TargetProteinGrams, daily.Protein),
                Carbohydrate: Compare(Plan.TargetCarbohydrateGrams, daily.Carbohydrate),
                Fat: Compare(Plan.TargetFatGrams, daily.Fat));
        }
    }

    private static NutrientComparisonResponse? Compare(decimal? target, NutrientTotal planned) =>
        target is { } value ? NutrientComparisonResponse.From(value, planned) : null;
}

