using NogVita.Domain.Common;

namespace NogVita.Domain.NutritionPlans;

public class MealFood : Entity
{
    public const decimal MaxQuantityInGrams = 2000m;

    public Guid MealId { get; private set; }
    public Guid FoodId { get; private set; }
    public decimal QuantityInGrams { get; private set; }
    public FoodSnapshot Snapshot { get; private set; } = null!;

    private MealFood() { } // EF Core

    internal MealFood(Guid mealId, Guid foodId, FoodSnapshot snapshot, decimal quantityInGrams)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        MealId = mealId;
        FoodId = foodId;
        Snapshot = snapshot;
        SetQuantity(quantityInGrams);
    }

    internal void SetQuantity(decimal quantityInGrams)
    {
        if (quantityInGrams <= 0)
        {
            throw new DomainException("A quantidade precisa ser maior que zero.");
        }
        if (quantityInGrams > MaxQuantityInGrams)
        {
            throw new DomainException($"A quantidade pode ser no máximo {MaxQuantityInGrams} g.");
        }
        QuantityInGrams = Math.Round(quantityInGrams, 2);
    }
}