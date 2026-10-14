using NogVita.Domain.Common;

namespace NogVita.Domain.NutritionPlans;

public class Meal : Entity
{
    public const int NameMaxLength = 60;
    public const int NotesMaxLength = 500;

    public Guid NutritionPlanId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public int Order { get; private set; }

    private Meal() { } // EF Core

    internal Meal(Guid nutritionPlanId, string name, string? notes, int order)
    {
        NutritionPlanId = nutritionPlanId;
        SetDetails(name, notes);
        Order = order;
    }

    internal void Update(string name, string? notes) => SetDetails(name, notes);

    internal void SetOrder(int order) => Order = order;

    private void SetDetails(string name, string? notes)
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

        if (notes != null)
        {
            notes = notes.Trim();
            if (notes == string.Empty)
            {
                notes = null;
            }
        }

        if (notes != null && notes.Length > NotesMaxLength)
        {
            throw new DomainException($"As notas da refeição não podem ter mais de {NotesMaxLength} caracteres.");
        }

        Name = normalizedName;
        Notes = notes;
    }
}