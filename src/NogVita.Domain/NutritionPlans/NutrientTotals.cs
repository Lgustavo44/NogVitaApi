namespace NogVita.Domain.NutritionPlans;

public sealed record NutrientTotal(decimal Value, bool IsComplete);

public sealed record NutrientTotals(
    NutrientTotal EnergyKcal,
    NutrientTotal Protein,
    NutrientTotal Carbohydrate,
    NutrientTotal Fat,
    NutrientTotal Fiber,
    NutrientTotal SodiumMg)
{
    public static NutrientTotals Zero { get; } = FromPortions([]);

    public static NutrientTotals FromPortions(IEnumerable<NutrientAmounts> portions)
    {
        var list = portions.ToList();

        return new NutrientTotals(
            Sum(list.Select(p => p.EnergyKcal)),
            Sum(list.Select(p => p.Protein)),
            Sum(list.Select(p => p.Carbohydrate)),
            Sum(list.Select(p => p.Fat)),
            Sum(list.Select(p => p.Fiber)),
            Sum(list.Select(p => p.SodiumMg)));
    }

    public static NutrientTotals Combine(IEnumerable<NutrientTotals> totals)
    {
        var list = totals.ToList();

        return new NutrientTotals(
            Combine(list.Select(t => t.EnergyKcal)),
            Combine(list.Select(t => t.Protein)),
            Combine(list.Select(t => t.Carbohydrate)),
            Combine(list.Select(t => t.Fat)),
            Combine(list.Select(t => t.Fiber)),
            Combine(list.Select(t => t.SodiumMg)));
    }

    private static NutrientTotal Sum(IEnumerable<decimal?> values)
    {
        var value = values.Sum(v => v ?? 0);
        var isComplete = values.All(v => v is not null);

        return new NutrientTotal(value, isComplete);
    }

    private static NutrientTotal Combine(IEnumerable<NutrientTotal> totals)
    {
        var value = totals.Sum(t => t.Value);
        var isComplete = totals.All(t => t.IsComplete);

        return new NutrientTotal(value, isComplete);
    }
}
