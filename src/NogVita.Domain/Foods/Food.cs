using NogVita.Domain.Common;

namespace NogVita.Domain.Foods;

public class Food : Entity
{
    public const int NameMaxLength = 200;
    public const int BrandMaxLength = 100;
    public const int CategoryMaxLength = 100;
    public const int SourceReferenceMaxLength = 20;

    public FoodSource Source { get; private set; }
    public string SourceReference { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public string? Barcode { get; private set; }
    public string? Category { get; private set; }
    public NutrientsPer100g Nutrients { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private Food() { } // EF Core

    private Food(
        FoodSource source,
        string sourceReference,
        string name,
        string? brand,
        string? barcode,
        string? category,
        NutrientsPer100g nutrients)
    {
        ArgumentNullException.ThrowIfNull(nutrients);

        Source = source;
        SourceReference = sourceReference;
        Name = NormalizeRequired(name, NameMaxLength, "O nome do alimento");
        Brand = NormalizeOptional(brand, BrandMaxLength, "A marca");
        Barcode = barcode;
        Category = NormalizeOptional(category, CategoryMaxLength, "A categoria");
        Nutrients = nutrients;
        IsActive = true;
    }

    public static Food FromTaco(int tacoNumber, string name, string? category, NutrientsPer100g nutrients)
    {
        if (tacoNumber <= 0)
        {
            throw new DomainException("O número do alimento na TACO precisa ser positivo.");
        }

        return new Food(FoodSource.Taco, tacoNumber.ToString(), name, brand: null, barcode: null, category, nutrients);
    }

    public static Food FromOpenFoodFacts(string barcode, string name, string? brand, NutrientsPer100g nutrients)
    {
        if (!Foods.Barcode.IsValid(barcode))
        {
            throw new DomainException("Código de barras inválido.");
        }

        return new Food(FoodSource.OpenFoodFacts, barcode, name, brand, barcode, category: null, nutrients);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string NormalizeRequired(string value, int maxLength, string field)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrEmpty(normalized))
        {
            throw new DomainException($"{field} é obrigatório.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{field} pode ter no máximo {maxLength} caracteres.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string field)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        if (normalized is { Length: var length } && length > maxLength)
        {
            throw new DomainException($"{field} pode ter no máximo {maxLength} caracteres.");
        }

        return normalized;
    }
}