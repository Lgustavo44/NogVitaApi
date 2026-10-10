using NogVita.Application.Foods;
using NogVita.Domain.Foods;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Foods;

public class ImportProductUseCaseTests
{
    private const string ValidBarcode = "7891000100103";

    private readonly FakeFoodRepository _foodRepository = new();
    private readonly FakeProductCatalog _catalog = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ImportProductUseCase CreateUseCase() => new(_foodRepository, _catalog, _unitOfWork);

    private static ExternalProduct CreateProduct(decimal? protein = 6m) =>
        new(ValidBarcode, "Biscoito Recheado", "Marca X", 480m, protein, 70m, 20m, 2m, 300m);

    [Fact]
    public async Task Should_Import_And_Save()
    {
        _catalog.Products[ValidBarcode] = CreateProduct();

        var result = await CreateUseCase().ExecuteAsync(ValidBarcode, TestContext.Current.CancellationToken);

        Assert.Equal(ImportProductStatus.Imported, result.Status);
        var food = Assert.Single(_foodRepository.Foods);
        Assert.Equal(FoodSource.OpenFoodFacts, food.Source);
        Assert.Equal(ValidBarcode, food.SourceReference);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
        Assert.Contains("ODbL", result.Food!.Attribution);
    }

    [Fact]
    public async Task Should_Not_Import_Twice_Nor_Call_Catalog()
    {
        _catalog.Products[ValidBarcode] = CreateProduct();
        var useCase = CreateUseCase();
        await useCase.ExecuteAsync(ValidBarcode, TestContext.Current.CancellationToken);
        var callsAfterFirstImport = _catalog.CallCount;

        var second = await useCase.ExecuteAsync(ValidBarcode, TestContext.Current.CancellationToken);

        Assert.Equal(ImportProductStatus.AlreadyImported, second.Status);
        Assert.Single(_foodRepository.Foods);
        Assert.Equal(callsAfterFirstImport, _catalog.CallCount);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Reject_Impossible_Data_From_Catalog()
    {
        _catalog.Products[ValidBarcode] = CreateProduct(protein: 150m);

        var result = await CreateUseCase().ExecuteAsync(ValidBarcode, TestContext.Current.CancellationToken);

        Assert.Equal(ImportProductStatus.InvalidData, result.Status);
        Assert.Empty(_foodRepository.Foods);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Catalog_Does_Not_Have_It()
    {
        var result = await CreateUseCase().ExecuteAsync(ValidBarcode, TestContext.Current.CancellationToken);

        Assert.Equal(ImportProductStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Should_Return_Unavailable_When_Catalog_Is_Down()
    {
        _catalog.IsUnavailable = true;

        var result = await CreateUseCase().ExecuteAsync(ValidBarcode, TestContext.Current.CancellationToken);

        Assert.Equal(ImportProductStatus.Unavailable, result.Status);
        Assert.Empty(_foodRepository.Foods);
    }

    [Fact]
    public async Task Should_Not_Reimport_Deactivated_Product()
    {
        _catalog.Products[ValidBarcode] = CreateProduct();
        var food = Food.FromOpenFoodFacts(ValidBarcode, "Produto com dado errado", null, new NutrientsPer100g(1m, 1m, 1m, 1m, 1m, 1m));
        food.Deactivate();
        _foodRepository.Add(food);

        var result = await CreateUseCase().ExecuteAsync(ValidBarcode, TestContext.Current.CancellationToken);

        Assert.Equal(ImportProductStatus.NotFound, result.Status);
        Assert.Single(_foodRepository.Foods);
        Assert.Equal(0, _catalog.CallCount);
    }
}