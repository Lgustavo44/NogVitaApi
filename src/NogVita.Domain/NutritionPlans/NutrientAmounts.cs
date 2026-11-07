namespace NogVita.Domain.NutritionPlans;

public sealed record NutrientAmounts(
    decimal? EnergyKcal,
    decimal? Protein,
    decimal? Carbohydrate,
    decimal? Fat,
    decimal? Fiber,
    decimal? SodiumMg)
{
    // A diferença nutriente a nutriente (sem informação de um dos lados: sem diferença).
    public NutrientAmounts Minus(NutrientAmounts other) => new(
        Diff(EnergyKcal, other.EnergyKcal),
        Diff(Protein, other.Protein),
        Diff(Carbohydrate, other.Carbohydrate),
        Diff(Fat, other.Fat),
        Diff(Fiber, other.Fiber),
        Diff(SodiumMg, other.SodiumMg));

    private static decimal? Diff(decimal? a, decimal? b) => a is null || b is null ? null : a - b;
}
