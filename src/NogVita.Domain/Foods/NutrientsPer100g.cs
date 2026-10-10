using NogVita.Domain.Common;

namespace NogVita.Domain.Foods;

public sealed record NutrientsPer100g
{
    public const decimal MaxGrams = 100m;
    public const decimal MaxEnergyKcal = 900m;
    public const decimal MaxSodiumMg = 40_000m;

    public decimal? EnergyKcal { get; private init; }
    public decimal? Protein { get; private init; }
    public decimal? Carbohydrate { get; private init; }
    public decimal? Fat { get; private init; }
    public decimal? Fiber { get; private init; }
    public decimal? SodiumMg { get; private init; }

    private NutrientsPer100g() { } // EF Core

    public NutrientsPer100g(
        decimal? energyKcal,
        decimal? protein,
        decimal? carbohydrate,
        decimal? fat,
        decimal? fiber,
        decimal? sodiumMg)
    {
        EnergyKcal = Validate(energyKcal, MaxEnergyKcal, "energia");
        Protein = Validate(protein, MaxGrams, "proteína");
        Carbohydrate = Validate(carbohydrate, MaxGrams, "carboidrato");
        Fat = Validate(fat, MaxGrams, "gordura");
        Fiber = Validate(fiber, MaxGrams, "fibra");
        SodiumMg = Validate(sodiumMg, MaxSodiumMg, "sódio");
    }

    public bool IsComplete =>
        EnergyKcal is not null && Protein is not null && Carbohydrate is not null && Fat is not null && Fiber is not null;

    private static decimal? Validate(decimal? value, decimal max, string nutrient)
    {
        if (value is null) return null;
        if (value < 0) throw new DomainException($"O valor de {nutrient} não pode ser negativo.");
        if (value > max) throw new DomainException($"O valor de {nutrient} está acima do limite possível para 100 g.");
        return value;
    }
}