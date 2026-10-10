using System.Text.Json.Serialization;

namespace NogVita.Infrastructure.Foods.OpenFoodFacts;

internal sealed class OpenFoodFactsResponse
{
    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("product")]
    public OpenFoodFactsProduct? Product { get; init; }
}

internal sealed class OpenFoodFactsProduct
{
    [JsonPropertyName("product_name_pt")]
    public string? ProductNamePt { get; init; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; init; }

    [JsonPropertyName("brands")]
    public string? Brands { get; init; }

    [JsonPropertyName("nutriments")]
    public OpenFoodFactsNutriments? Nutriments { get; init; }
}

internal sealed class OpenFoodFactsNutriments
{
    [JsonPropertyName("energy-kcal_100g")]
    public decimal? EnergyKcal { get; init; }

    [JsonPropertyName("proteins_100g")]
    public decimal? Proteins { get; init; }

    [JsonPropertyName("carbohydrates_100g")]
    public decimal? Carbohydrates { get; init; }

    [JsonPropertyName("fat_100g")]
    public decimal? Fat { get; init; }

    [JsonPropertyName("fiber_100g")]
    public decimal? Fiber { get; init; }

    [JsonPropertyName("sodium_100g")]
    public decimal? SodiumGrams { get; init; }
}