namespace NogVita.Application.Foods;

public sealed record ExternalProduct(
    string Barcode,
    string Name,
    string? Brand,
    decimal? EnergyKcalPer100g,
    decimal? ProteinPer100g,
    decimal? CarbohydratePer100g,
    decimal? FatPer100g,
    decimal? FiberPer100g,
    decimal? SodiumMgPer100g);