using NogVita.Domain.Foods;
using NogVita.Infrastructure.Persistence.Seed;

namespace NogVita.UnitTests.Infrastructure.Foods;

public class OpenFoodFactsCsvReaderTests
{
    // Passa cada linha do CSV pelo domínio de verdade: se o filtro do DuckDB esquecer alguma regra, o teste mostra qual produto.
    [Fact]
    public void Every_Row_Should_Be_Accepted_By_The_Domain()
    {
        var rows = OpenFoodFactsCsvReader.ReadRows().ToList();

        Assert.NotEmpty(rows);
        Assert.Equal(rows.Count, rows.Select(r => r.Code).Distinct().Count());

        foreach (var row in rows)
        {
            var nutrients = new NutrientsPer100g(
                row.EnergyKcal, row.Protein, row.Carbohydrate, row.Fat, row.Fiber, row.SodiumMg);

            var food = Food.FromOpenFoodFacts(row.Code, row.Name, row.Brand, nutrients);

            Assert.Equal(FoodSource.OpenFoodFacts, food.Source);
        }
    }

    [Fact]
    public void Should_Keep_Leading_Zeros_Of_The_Barcode()
    {
        var row = OpenFoodFactsCsvReader.ReadRows().First(r => r.Code == "0000200375991");

        Assert.Equal("SADIA", row.Brand);
        Assert.Equal(18.3333m, row.Protein);
    }
}
