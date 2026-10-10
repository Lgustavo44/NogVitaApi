using NogVita.Application.Foods;
using NogVita.Domain.Foods;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Foods;

public class LookupProductByBarcodeUseCaseTests
{
    private readonly FakeFoodRepository _foodRepository = new();
    private readonly FakeProductCatalog _catalog = new();

    private LookupProductByBarcodeUseCase CreateUseCase() => new(_foodRepository, _catalog);

    private static ExternalProduct CreateProduct(string barcode) =>
        new(barcode, "Biscoito de Teste", "Marca Teste", 480m, 6m, 70m, 20m, 2m, 300m);

    private static Food CreateImportedFood(string barcode) =>
        Food.FromOpenFoodFacts(barcode, "Biscoito Importado", "Marca Teste", new NutrientsPer100g(480m, 6m, 70m, 20m, 2m, 300m));

    [Fact]
    public async Task Should_Return_Product_When_Found()
    {
        _catalog.Products["7891000100103"] = CreateProduct("7891000100103");

        var result = await CreateUseCase().ExecuteAsync("7891000100103", TestContext.Current.CancellationToken);

        Assert.Equal(LookupProductStatus.Found, result.Status);
        Assert.Equal("Biscoito de Teste", result.Preview!.Name);
        Assert.False(result.Preview.AlreadyImported);
        Assert.Null(result.Preview.FoodId);
    }

    [Fact]
    public async Task Should_Use_Local_Food_Without_Calling_Catalog()
    {
        var food = CreateImportedFood("7891000100103");
        _foodRepository.Add(food);

        var result = await CreateUseCase().ExecuteAsync("7891000100103", TestContext.Current.CancellationToken);

        Assert.Equal(LookupProductStatus.Found, result.Status);
        Assert.True(result.Preview!.AlreadyImported);
        Assert.Equal(food.Id, result.Preview.FoodId);
        Assert.Equal("Biscoito Importado", result.Preview.Name);
        Assert.Equal(0, _catalog.CallCount);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Local_Food_Is_Inactive()
    {
        var food = CreateImportedFood("7891000100103");
        food.Deactivate();
        _foodRepository.Add(food);
        _catalog.Products["7891000100103"] = CreateProduct("7891000100103");

        var result = await CreateUseCase().ExecuteAsync("7891000100103", TestContext.Current.CancellationToken);

        Assert.Equal(LookupProductStatus.NotFound, result.Status);
        Assert.Equal(0, _catalog.CallCount);
    }

    [Fact]
    public async Task Should_Not_Call_Catalog_When_Barcode_Is_Invalid()
    {
        var result = await CreateUseCase().ExecuteAsync("abc", TestContext.Current.CancellationToken);

        Assert.Equal(LookupProductStatus.InvalidBarcode, result.Status);
        Assert.Equal(0, _catalog.CallCount);
    }

    [Fact]
    public async Task Should_Return_NotFound()
    {
        var result = await CreateUseCase().ExecuteAsync("7891000100103", TestContext.Current.CancellationToken);

        Assert.Equal(LookupProductStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Should_Return_Unavailable_When_Catalog_Is_Down()
    {
        _catalog.IsUnavailable = true;

        var result = await CreateUseCase().ExecuteAsync("7891000100103", TestContext.Current.CancellationToken);

        Assert.Equal(LookupProductStatus.Unavailable, result.Status);
    }
}
