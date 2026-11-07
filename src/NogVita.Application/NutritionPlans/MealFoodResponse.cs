using System.Text.Json.Serialization;
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

    // A medida caseira escolhida ("2 × unidade média"), ou null quando a quantidade foi dada em gramas (D-V2-34).
    public MeasureResponse? Measure { get; init; }

    // D-V2-36: as alternativas deste item ("ou"). Só os itens principais têm alternativas, e só eles entram nos totais.
    public IReadOnlyList<MealFoodResponse> Substitutes { get; init; } = [];

    [JsonIgnore]
    public Guid? SubstituteForId { get; init; }

    internal NutrientAmounts? MainRawPortion { get; init; }

    // Numa alternativa: quanto ela tem a mais (ou a menos) que o item principal. Nos itens principais, null.
    public NutrientAmountsResponse? Difference =>
        MainRawPortion is null ? null : NutrientAmountsResponse.From(RawPortion.Minus(MainRawPortion));

    // Agrupa a lista plana da consulta: os principais, cada um com as suas alternativas, na ordem de entrada.
    public static IReadOnlyList<MealFoodResponse> GroupSubstitutes(IReadOnlyList<MealFoodResponse> items) =>
        items.Where(i => i.SubstituteForId is null)
            .Select(main => main with
            {
                Substitutes = items
                    .Where(i => i.SubstituteForId == main.Id)
                    .Select(sub => sub with { MainRawPortion = main.RawPortion })
                    .ToList()
            })
            .ToList();

    // Sem os macros (D-V2-25), também nas alternativas e na diferença.
    internal MealFoodResponse WithoutMacros()
    {
        var main = this with { ProteinPer100g = null, CarbohydratePer100g = null, FatPer100g = null };

        return main with
        {
            Substitutes = Substitutes
                .Select(sub => sub with { ProteinPer100g = null, CarbohydratePer100g = null, FatPer100g = null, MainRawPortion = main.RawPortion })
                .ToList()
        };
    }
}

public sealed record MeasureResponse(string Name, decimal Grams, decimal Quantity);
