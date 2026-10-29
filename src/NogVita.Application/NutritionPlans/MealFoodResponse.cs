using NogVita.Application.Foods;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

public sealed record MealFoodResponse(
    Guid Id,
    Guid FoodId,
    decimal QuantityInGrams,
    string Name,
    string? Brand,
    FoodSource Source,
    string SourceReference,
    DateTime SnapshotTakenAtUtc,
    decimal? EnergyKcalPer100g,
    decimal? ProteinPer100g,
    decimal? CarbohydratePer100g,
    decimal? FatPer100g,
    decimal? FiberPer100g,
    decimal? SodiumMgPer100g)
{
    public string Attribution => FoodSourceInfo.Attribution(Source);

    internal NutrientAmounts RawPortion => NutritionCalculator.Portion(
        new NutrientsPer100g(EnergyKcalPer100g, ProteinPer100g, CarbohydratePer100g, FatPer100g, FiberPer100g, SodiumMgPer100g),
        QuantityInGrams);

    public NutrientAmountsResponse Portion => NutrientAmountsResponse.From(RawPortion);
}
