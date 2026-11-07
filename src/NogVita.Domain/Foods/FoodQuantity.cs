using NogVita.Domain.Common;

namespace NogVita.Domain.Foods;

// Quanto de um alimento: em gramas, ou em porções ("2 × unidade média"). O cálculo usa sempre Grams; a porção
// é só como a quantidade foi escolhida e é mostrada (D-V2-34), guardada como retrato: mudar a tabela de porções
// não muda um plano nem um diário antigo.
public sealed record FoodQuantity
{
    public const decimal MaxGrams = 2000m;
    public const decimal MaxPortionQuantity = 50m;

    public decimal Grams { get; }
    public string? PortionName { get; }
    public decimal? PortionGrams { get; }
    public decimal? PortionQuantity { get; }

    private FoodQuantity(decimal grams, string? portionName, decimal? portionGrams, decimal? portionQuantity)
    {
        if (grams <= 0)
        {
            throw new DomainException("A quantidade precisa ser maior que zero.");
        }

        if (grams > MaxGrams)
        {
            throw new DomainException($"A quantidade pode ser no máximo {MaxGrams} g.");
        }

        Grams = Math.Round(grams, 2);
        PortionName = portionName;
        PortionGrams = portionGrams;
        PortionQuantity = portionQuantity;
    }

    public static FoodQuantity InGrams(decimal grams) => new(grams, null, null, null);

    public static FoodQuantity OfPortion(FoodPortion portion, decimal quantity)
    {
        ArgumentNullException.ThrowIfNull(portion);

        if (quantity is <= 0 or > MaxPortionQuantity)
        {
            throw new DomainException($"A quantidade de porções precisa ser maior que zero e no máximo {MaxPortionQuantity}.");
        }

        return new FoodQuantity(portion.Grams * quantity, portion.Name, portion.Grams, Math.Round(quantity, 2));
    }

    // Reconstrói o retrato guardado (o EF e as colunas jsonb), sem refazer a conta.
    public static FoodQuantity Restore(decimal grams, string? portionName, decimal? portionGrams, decimal? portionQuantity) =>
        new(grams, portionName, portionGrams, portionQuantity);
}
