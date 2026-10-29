using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

public static class NutritionRounding
{
    public static decimal Round(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    public static decimal? Round(decimal? value) => value is null ? null : Round(value.Value);
}

public sealed record NutrientAmountsResponse(
    decimal? EnergyKcal,
    decimal? Protein,
    decimal? Carbohydrate,
    decimal? Fat,
    decimal? Fiber,
    decimal? SodiumMg)
{
    public static NutrientAmountsResponse From(NutrientAmounts amounts) => new(
        NutritionRounding.Round(amounts.EnergyKcal),
        NutritionRounding.Round(amounts.Protein),
        NutritionRounding.Round(amounts.Carbohydrate),
        NutritionRounding.Round(amounts.Fat),
        NutritionRounding.Round(amounts.Fiber),
        NutritionRounding.Round(amounts.SodiumMg));
}

public sealed record NutrientTotalResponse(decimal Value, bool IsComplete)
{
    public static NutrientTotalResponse From(NutrientTotal total) =>
        new(NutritionRounding.Round(total.Value), total.IsComplete);
}

public sealed record NutrientTotalsResponse(
    NutrientTotalResponse EnergyKcal,
    NutrientTotalResponse Protein,
    NutrientTotalResponse Carbohydrate,
    NutrientTotalResponse Fat,
    NutrientTotalResponse Fiber,
    NutrientTotalResponse SodiumMg)
{
    public static NutrientTotalsResponse From(NutrientTotals totals) => new(
        NutrientTotalResponse.From(totals.EnergyKcal),
        NutrientTotalResponse.From(totals.Protein),
        NutrientTotalResponse.From(totals.Carbohydrate),
        NutrientTotalResponse.From(totals.Fat),
        NutrientTotalResponse.From(totals.Fiber),
        NutrientTotalResponse.From(totals.SodiumMg));
}

public sealed record NutrientComparisonResponse(decimal Target, decimal Planned, decimal Difference, bool IsComplete)
{
    public static NutrientComparisonResponse From(decimal target, NutrientTotal planned) => new(
        NutritionRounding.Round(target),
        NutritionRounding.Round(planned.Value),
        NutritionRounding.Round(planned.Value - target),
        planned.IsComplete);
}

public sealed record TargetComparisonResponse(
    NutrientComparisonResponse EnergyKcal,
    NutrientComparisonResponse? Protein,
    NutrientComparisonResponse? Carbohydrate,
    NutrientComparisonResponse? Fat);
