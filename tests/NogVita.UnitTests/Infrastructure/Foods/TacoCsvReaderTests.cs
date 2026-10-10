using NogVita.Domain.Foods;
using NogVita.Infrastructure.Persistence.Seed;

namespace NogVita.UnitTests.Infrastructure.Foods;

public class TacoCsvReaderTests
{
    [Fact]
    public void Should_Read_All_597_Foods_From_Embedded_Csv()
    {
        var foods = TacoCsvReader.ReadFoods();

        Assert.Equal(597, foods.Count);
        Assert.All(foods, f => Assert.Equal(FoodSource.Taco, f.Source));
        Assert.Equal(597, foods.Select(f => f.SourceReference).Distinct().Count());
    }

    [Fact]
    public void Should_Read_Rice_With_Official_Values()
    {
        var rice = TacoCsvReader.ReadFoods().Single(f => f.Name == "Arroz, tipo 1, cozido");

        Assert.Equal(128m, Math.Round(rice.Nutrients.EnergyKcal!.Value));
        Assert.Equal(2.5m, Math.Round(rice.Nutrients.Protein!.Value, 1));
        Assert.Equal(28.1m, Math.Round(rice.Nutrients.Carbohydrate!.Value, 1));
        Assert.Null(rice.Brand);
        Assert.Null(rice.Barcode);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("1e-05", 0d)]
    [InlineData("128.26", 128.26)]
    [InlineData("70.13866666666667", 70.1387)]
    public void Should_Parse_Nutrient(string raw, double? expected)
    {
        var value = TacoCsvReader.ParseNutrient(raw);

        Assert.Equal(expected is null ? null : (decimal?)expected.Value, value);
    }
}