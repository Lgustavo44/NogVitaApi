using NogVita.Domain.Common;
using NogVita.Domain.Foods;

namespace NogVita.Domain.NutritionPlans;

public class Meal : Entity
{
    public const int NameMaxLength = 60;
    public const int NotesMaxLength = 500;
    public const int MaxFoods = 20;

    private readonly List<MealFood> _foods = [];

    public Guid NutritionPlanId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public int Order { get; private set; }

    // Horário sugerido, opcional. Não define a ordem: o jantar de quem trabalha à noite pode ser às 3h.
    public TimeOnly? SuggestedTime { get; private set; }

    public IReadOnlyCollection<MealFood> Foods => _foods.AsReadOnly();

    private Meal() { } // EF Core

    internal Meal(Guid nutritionPlanId, string name, string? notes, TimeOnly? suggestedTime, int order)
    {
        NutritionPlanId = nutritionPlanId;
        SetDetails(name, notes, suggestedTime);
        Order = order;
    }

    // Dados da refeição

    internal void Update(string name, string? notes, TimeOnly? suggestedTime) => SetDetails(name, notes, suggestedTime);

    internal void SetOrder(int order) => Order = order;

    // Alimentos da refeição

    internal MealFood AddFood(Food food, decimal quantityInGrams, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(food);

        // RN19
        if (!food.IsActive)
        {
            throw new DomainException("Um alimento inativo não pode ser adicionado ao plano.");
        }

        // D141: o mesmo alimento entra uma vez só; para mudar, altera-se a quantidade.
        if (_foods.Any(f => f.FoodId == food.Id))
        {
            throw new DomainException("Este alimento já está na refeição. Altere a quantidade dele.");
        }

        // D142
        if (_foods.Count >= MaxFoods)
        {
            throw new DomainException($"Uma refeição pode ter no máximo {MaxFoods} alimentos.");
        }

        var mealFood = new MealFood(Id, food.Id, FoodSnapshot.From(food, nowUtc), quantityInGrams);
        _foods.Add(mealFood);
        return mealFood;
    }

    internal void UpdateFoodQuantity(Guid mealFoodId, decimal quantityInGrams) =>
        GetFood(mealFoodId).SetQuantity(quantityInGrams);

    internal void RemoveFood(Guid mealFoodId) =>
        _foods.Remove(GetFood(mealFoodId));

    private void SetDetails(string name, string? notes, TimeOnly? suggestedTime)
    {
        var normalizedName = name?.Trim();

        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new DomainException("O nome da refeição é obrigatório.");
        }

        if (normalizedName.Length > NameMaxLength)
        {
            throw new DomainException($"O nome da refeição não pode ter mais de {NameMaxLength} caracteres.");
        }

        var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (normalizedNotes is { Length: > NotesMaxLength })
        {
            throw new DomainException($"As notas da refeição não podem ter mais de {NotesMaxLength} caracteres.");
        }

        Name = normalizedName;
        Notes = normalizedNotes;
        SuggestedTime = suggestedTime;
    }

    private MealFood GetFood(Guid mealFoodId) =>
        _foods.FirstOrDefault(f => f.Id == mealFoodId)
            ?? throw new DomainException("Alimento não encontrado nesta refeição.");
}
