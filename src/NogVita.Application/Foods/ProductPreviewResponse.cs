using NogVita.Domain.Foods;

namespace NogVita.Application.Foods;

public sealed record ProductPreviewResponse(
    bool AlreadyImported,
    Guid? FoodId,
    string Barcode,
    string Name,
    string? Brand,
    decimal? EnergyKcalPer100g,
    decimal? ProteinPer100g,
    decimal? CarbohydratePer100g,
    decimal? FatPer100g,
    decimal? FiberPer100g,
    decimal? SodiumMgPer100g)
{
    public string Attribution => FoodSourceInfo.Attribution(FoodSource.OpenFoodFacts);

    public static ProductPreviewResponse FromFood(Food food) => new(
        AlreadyImported: true,
        FoodId: food.Id,
        Barcode: food.Barcode!,
        Name: food.Name,
        Brand: food.Brand,
        EnergyKcalPer100g: food.Nutrients.EnergyKcal,
        ProteinPer100g: food.Nutrients.Protein,
        CarbohydratePer100g: food.Nutrients.Carbohydrate,
        FatPer100g: food.Nutrients.Fat,
        FiberPer100g: food.Nutrients.Fiber,
        SodiumMgPer100g: food.Nutrients.SodiumMg);

    public static ProductPreviewResponse FromExternal(ExternalProduct product) => new(
        AlreadyImported: false,
        FoodId: null,
        Barcode: product.Barcode,
        Name: product.Name,
        Brand: product.Brand,
        EnergyKcalPer100g: product.EnergyKcalPer100g,
        ProteinPer100g: product.ProteinPer100g,
        CarbohydratePer100g: product.CarbohydratePer100g,
        FatPer100g: product.FatPer100g,
        FiberPer100g: product.FiberPer100g,
        SodiumMgPer100g: product.SodiumMgPer100g);
}