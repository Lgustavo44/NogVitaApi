using NogVita.Domain.Foods;

namespace NogVita.Application.Foods;

public sealed record FoodResponse(
    Guid Id,
    string Name,
    string? Brand,
    string? Barcode,
    string? Category,
    FoodSource Source,
    string SourceReference,
    bool IsActive,
    decimal? EnergyKcalPer100g,
    decimal? ProteinPer100g,
    decimal? CarbohydratePer100g,
    decimal? FatPer100g,
    decimal? FiberPer100g,
    decimal? SodiumMgPer100g)
{
    // Para entidades já carregadas. As consultas projetam direto no banco (FoodQueries.ToResponse): mantenha os dois iguais.
    public static FoodResponse From(Food food) => new(
        food.Id,
        food.Name,
        food.Brand,
        food.Barcode,
        food.Category,
        food.Source,
        food.SourceReference,
        food.IsActive,
        food.Nutrients.EnergyKcal,
        food.Nutrients.Protein,
        food.Nutrients.Carbohydrate,
        food.Nutrients.Fat,
        food.Nutrients.Fiber,
        food.Nutrients.SodiumMg);

    public string Attribution => FoodSourceInfo.Attribution(Source);

    public bool IsComplete =>
        EnergyKcalPer100g is not null
        && ProteinPer100g is not null
        && CarbohydratePer100g is not null
        && FatPer100g is not null
        && FiberPer100g is not null;
}