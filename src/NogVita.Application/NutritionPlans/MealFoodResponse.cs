using NogVita.Application.Foods;
using NogVita.Domain.Foods;

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
}