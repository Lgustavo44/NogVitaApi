using NogVita.Domain.Common;
using NogVita.Domain.Foods;

namespace NogVita.UnitTests.Domain.Foods;

public class FoodTests
{
    private static readonly NutrientsPer100g Nutrients = new(128m, 2.5m, 28m, 0.2m, 1.6m, 1m);

    [Fact]
    public void Should_Create_From_Taco_As_Generic_Food()
    {
        var food = Food.FromTaco(3, "  Arroz, tipo 1, cozido  ", "Cereais e derivados", Nutrients);

        Assert.Equal(FoodSource.Taco, food.Source);
        Assert.Equal("3", food.SourceReference);
        Assert.Equal("Arroz, tipo 1, cozido", food.Name);
        Assert.Null(food.Brand);
        Assert.Null(food.Barcode);
        Assert.True(food.IsActive);
    }

    [Fact]
    public void Should_Create_From_Open_Food_Facts_As_Product()
    {
        var food = Food.FromOpenFoodFacts("7891000100103", "Biscoito Recheado", "Marca X", Nutrients);

        Assert.Equal(FoodSource.OpenFoodFacts, food.Source);
        Assert.Equal("7891000100103", food.SourceReference);
        Assert.Equal("7891000100103", food.Barcode);
        Assert.Equal("Marca X", food.Brand);
    }

    [Fact]
    public void Should_Reject_Invalid_Barcode()
    {
        Assert.Throws<DomainException>(() => Food.FromOpenFoodFacts("abc", "Produto", null, Nutrients));
    }

    [Fact]
    public void Should_Reject_Empty_Name()
    {
        Assert.Throws<DomainException>(() => Food.FromTaco(3, "   ", null, Nutrients));
    }

    [Fact]
    public void Should_Reject_Invalid_Taco_Number()
    {
        Assert.Throws<DomainException>(() => Food.FromTaco(0, "Arroz", null, Nutrients));
    }

    [Fact]
    public void Should_Deactivate_And_Activate()
    {
        var food = Food.FromTaco(3, "Arroz", null, Nutrients);

        food.Deactivate();
        Assert.False(food.IsActive);

        food.Activate();
        Assert.True(food.IsActive);
    }
}