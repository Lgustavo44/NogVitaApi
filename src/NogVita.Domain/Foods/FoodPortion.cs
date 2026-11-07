using NogVita.Domain.Common;

namespace NogVita.Domain.Foods;

public enum FoodPortionSource
{
    Ibge,
    OpenFoodFacts
}

// D-V2-33: uma medida caseira do alimento ("colher de sopa cheia", "unidade média") e quantos gramas ela tem.
// Vem só de fontes automáticas (IBGE para a TACO, a porção da embalagem para o Open Food Facts): ninguém cria à mão.
public class FoodPortion : Entity
{
    public const int NameMaxLength = 80;
    public const decimal MaxGrams = 2000m;

    public Guid FoodId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Grams { get; private set; }
    public FoodPortionSource Source { get; private set; }

    private FoodPortion() { } // EF Core

    // A porção do Open Food Facts: a da embalagem, com o rótulo de lá ("porção da embalagem (30g)").
    public static string PackageName(string? label)
    {
        var name = string.IsNullOrWhiteSpace(label) ? "porção da embalagem" : $"porção da embalagem ({label.Trim()})";
        return name.Length <= NameMaxLength ? name : name[..NameMaxLength];
    }

    public FoodPortion(Guid foodId, string name, decimal grams, FoodPortionSource source)
    {
        var normalized = name?.Trim();

        if (string.IsNullOrEmpty(normalized) || normalized.Length > NameMaxLength)
        {
            throw new DomainException($"O nome da porção precisa ter de 1 a {NameMaxLength} caracteres.");
        }

        if (grams is <= 0 or > MaxGrams)
        {
            throw new DomainException($"Uma porção precisa ter de 0,01 a {MaxGrams} g.");
        }

        FoodId = foodId;
        Name = normalized;
        Grams = Math.Round(grams, 2);
        Source = source;
    }
}
