using System.Globalization;
using NogVita.Domain.Foods;
using NogVita.Infrastructure.Persistence.Seed;

namespace NogVita.UnitTests.Infrastructure.Persistence;

// D-V2-33: os CSVs embutidos das porções precisam entrar inteiros no seed (nenhuma linha recusada pelo domínio).
public class FoodPortionCsvTests
{
    [Fact]
    public void Taco_Portions_Should_Point_To_Taco_Foods_With_Valid_Grams_And_No_Duplicates()
    {
        var tacoNumbers = TacoCsvReader.ReadFoods().Select(f => f.SourceReference).ToHashSet();
        var rows = FoodPortionSeeder.ReadRows(FoodPortionSeeder.TacoResource).ToList();

        Assert.True(rows.Count > 1000);
        Assert.All(rows, row =>
        {
            Assert.Contains(row["numero_alimento"], tacoNumbers);
            AssertValid(row["porcao"], row["gramas"]);
        });
        Assert.Equal(rows.Count, rows.Select(r => (r["numero_alimento"], r["porcao"].ToUpperInvariant())).Distinct().Count());
    }

    [Fact]
    public void Open_Food_Facts_Portions_Should_Have_Valid_Grams_And_One_Per_Product()
    {
        var rows = FoodPortionSeeder.ReadRows(FoodPortionSeeder.OpenFoodFactsResource).ToList();

        Assert.True(rows.Count > 20000);
        Assert.All(rows, row => AssertValid(FoodPortion.PackageName(row["serving_label"]), row["serving_grams"]));
        Assert.Equal(rows.Count, rows.Select(r => r["code"]).Distinct().Count());
    }

    private static void AssertValid(string name, string grams) =>
        _ = new FoodPortion(Guid.NewGuid(), name, decimal.Parse(grams, CultureInfo.InvariantCulture), FoodPortionSource.Ibge);
}
