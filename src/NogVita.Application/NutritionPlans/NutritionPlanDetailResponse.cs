using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

// OutsideRoutine: aviso, e não bloqueio. true quando a refeição cai enquanto o paciente dorme;
// null quando o paciente não informou a rotina ou a refeição não tem horário.
public sealed record MealResponse(
    Guid Id,
    string Name,
    string? Notes,
    int Order,
    TimeOnly? SuggestedTime,
    IReadOnlyList<MealFoodResponse> Foods,
    bool? OutsideRoutine = null,
    bool MacrosVisible = true)
{
    internal NutrientTotals RawTotals => NutrientTotals.FromPortions(Foods.Select(f => f.RawPortion));

    public NutrientTotalsResponse Totals => NutrientTotalsResponse.From(RawTotals, MacrosVisible);
}

public sealed record NutritionPlanDetailResponse(NutritionPlanResponse Plan, IReadOnlyList<MealResponse> Meals, bool MacrosVisible = true)
{
    private NutrientTotals RawDailyTotals => NutrientTotals.Combine(Meals.Select(m => m.RawTotals));

    public NutrientTotalsResponse DailyTotals => NutrientTotalsResponse.From(RawDailyTotals, MacrosVisible);

    // D-V2-25: a visão do paciente. Os macros saem dos alimentos, dos totais e da meta; as kcal, as fibras e o sódio ficam.
    public NutritionPlanDetailResponse ForPatient() => Plan.PatientSeesMacros ? this : this with
    {
        MacrosVisible = false,
        Plan = Plan with { TargetProteinGrams = null, TargetCarbohydrateGrams = null, TargetFatGrams = null },
        Meals = Meals.Select(meal => meal with
        {
            MacrosVisible = false,
            Foods = meal.Foods.Select(food => food.WithoutMacros()).ToList()
        }).ToList()
    };

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

