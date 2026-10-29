using NogVita.Domain.Foods;

namespace NogVita.Domain.NutritionPlans;

public static class NutritionCalculator
{
    public static NutrientAmounts Portion(NutrientsPer100g per100g, decimal quantityInGrams)
    {
        ArgumentNullException.ThrowIfNull(per100g);

        var factor = quantityInGrams / 100m;

        return new NutrientAmounts(
            per100g.EnergyKcal * factor,
            per100g.Protein * factor,
            per100g.Carbohydrate * factor,
            per100g.Fat * factor,
            per100g.Fiber * factor,
            per100g.SodiumMg * factor);
    }
}
