using NogVita.Domain.Common;

namespace NogVita.Domain.NutritionPlans;

public sealed record PlanTarget
{
    public const decimal MaxEnergyKcal = 10_000m;
    public const decimal MaxMacroGrams = 1_000m;

    public decimal EnergyKcal { get; }
    public decimal? ProteinGrams { get; }
    public decimal? CarbohydrateGrams { get; }
    public decimal? FatGrams { get; }

    public PlanTarget(decimal energyKcal, decimal? proteinGrams, decimal? carbohydrateGrams, decimal? fatGrams)
    {
        if (energyKcal is <= 0 or > MaxEnergyKcal)
        {
            throw new DomainException($"A meta de calorias precisa estar entre 1 e {MaxEnergyKcal} kcal.");
        }

        if (proteinGrams is < 0 or > MaxMacroGrams)
        {
            throw new DomainException($"A meta de proteína precisa estar entre 0 e {MaxMacroGrams} g.");
        }
        ValidateMacro(carbohydrateGrams, "carboidrato");
        ValidateMacro(fatGrams, "gordura");

        EnergyKcal = energyKcal;
        ProteinGrams = proteinGrams;
        CarbohydrateGrams = carbohydrateGrams;
        FatGrams = fatGrams;
    }

    private static void ValidateMacro(decimal? value, string macroName)
    {
        if (value is < 0 or > MaxMacroGrams)
        {
            throw new DomainException($"A meta de {macroName} precisa estar entre 0 e {MaxMacroGrams} g.");
        }
    }
}