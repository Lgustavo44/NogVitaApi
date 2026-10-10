using NogVita.Domain.Foods;

namespace NogVita.Application.Foods;

public static class FoodSourceInfo
{
    public static string Attribution(FoodSource source) => source switch
    {
        FoodSource.Taco => "TACO 4ª edição, NEPA/UNICAMP",
        FoodSource.OpenFoodFacts => "Open Food Facts, licença ODbL",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Fonte de alimento desconhecida.")
    };
}