using NogVita.Domain.Common;
using NogVita.Domain.Foods;

namespace NogVita.Domain.NutritionPlans;

public class MealFood : Entity
{
    public const decimal MaxQuantityInGrams = FoodQuantity.MaxGrams;

    public Guid MealId { get; private set; }
    public Guid FoodId { get; private set; }
    public decimal QuantityInGrams { get; private set; }

    // O retrato da porção escolhida (D-V2-34). Nulos quando a quantidade foi dada em gramas.
    public string? PortionName { get; private set; }
    public decimal? PortionGrams { get; private set; }
    public decimal? PortionQuantity { get; private set; }

    public FoodSnapshot Snapshot { get; private set; } = null!;

    // D-V2-36: uma alternativa aponta para o item principal da mesma refeição ("frango OU patinho").
    public Guid? SubstituteForId { get; private set; }

    public bool IsSubstitute => SubstituteForId is not null;

    public FoodQuantity Quantity => FoodQuantity.Restore(QuantityInGrams, PortionName, PortionGrams, PortionQuantity);

    private MealFood() { } // EF Core

    internal MealFood(Guid mealId, Guid foodId, FoodSnapshot snapshot, FoodQuantity quantity, Guid? substituteForId = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        MealId = mealId;
        SubstituteForId = substituteForId;
        FoodId = foodId;
        Snapshot = snapshot;
        SetQuantity(quantity);
    }

    internal void SetQuantity(FoodQuantity quantity)
    {
        ArgumentNullException.ThrowIfNull(quantity);

        QuantityInGrams = quantity.Grams;
        PortionName = quantity.PortionName;
        PortionGrams = quantity.PortionGrams;
        PortionQuantity = quantity.PortionQuantity;
    }
}
