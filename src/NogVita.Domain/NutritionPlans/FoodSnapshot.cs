using NogVita.Domain.Foods;

namespace NogVita.Domain.NutritionPlans;

public sealed record FoodSnapshot
{
    public string Name { get; private init; } = string.Empty;
    public string? Brand { get; private init; }
    public FoodSource Source { get; private init; }
    public string SourceReference { get; private init; } = string.Empty;
    public NutrientsPer100g Nutrients { get; private init; } = null!;
    public DateTime TakenAtUtc { get; private init; }

    private FoodSnapshot() { } // EF Core

    public static FoodSnapshot From(Food food, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(food);

        return new FoodSnapshot
        {
            Name = food.Name,
            Brand = food.Brand,
            Source = food.Source,
            SourceReference = food.SourceReference,
            Nutrients = food.Nutrients with { },
            TakenAtUtc = nowUtc
        };
    }
}