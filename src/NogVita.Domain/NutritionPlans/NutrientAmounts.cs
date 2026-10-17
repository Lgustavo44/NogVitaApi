namespace NogVita.Domain.NutritionPlans;

public sealed record NutrientAmounts(
    decimal? EnergyKcal,
    decimal? Protein,
    decimal? Carbohydrate,
    decimal? Fat,
    decimal? Fiber,
    decimal? SodiumMg);