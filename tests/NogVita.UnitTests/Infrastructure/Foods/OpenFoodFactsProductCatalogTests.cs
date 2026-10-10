using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Foods;
using NogVita.Infrastructure.Foods.OpenFoodFacts;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Infrastructure.Foods;

public class OpenFoodFactsProductCatalogTests
{
    private const string Barcode = "7891000100103";

    private static (OpenFoodFactsProductCatalog Catalog, StubHttpMessageHandler Handler) CreateCatalog(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHttpMessageHandler(respond);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://world.openfoodfacts.org/") };
        var catalog = new OpenFoodFactsProductCatalog(httpClient, NullLogger<OpenFoodFactsProductCatalog>.Instance);
        return (catalog, handler);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Should_Map_Product_And_Normalize_Values()
    {
        var (catalog, handler) = CreateCatalog(_ => Json("""
            {
              "status": 1,
              "product": {
                "product_name_pt": "  Biscoito Recheado  ",
                "product_name": "Stuffed Cookie",
                "brands": ",Marca Principal, Outra Marca",
                "nutriments": {
                  "energy_100g": 2000,
                  "energy-kcal_100g": 480,
                  "proteins_100g": "6.5",
                  "carbohydrates_100g": 70,
                  "fat_100g": 20,
                  "fiber_100g": 2,
                  "sodium_100g": 0.3
                }
              }
            }
            """));

        var result = await catalog.GetByBarcodeAsync(Barcode, TestContext.Current.CancellationToken);

        Assert.Equal(ProductLookupStatus.Found, result.Status);
        var product = result.Product!;
        Assert.Equal(Barcode, product.Barcode);
        Assert.Equal("Biscoito Recheado", product.Name);
        Assert.Equal("Marca Principal", product.Brand);
        Assert.Equal(480m, product.EnergyKcalPer100g);
        Assert.Equal(6.5m, product.ProteinPer100g);
        Assert.Equal(300m, product.SodiumMgPer100g);

        Assert.Contains($"api/v2/product/{Barcode}", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("fields=", handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async Task Should_Use_Default_Name_When_Portuguese_Name_Is_Missing()
    {
        var (catalog, _) = CreateCatalog(_ => Json("""
            { "status": 1, "product": { "product_name_pt": "", "product_name": "Chocolate" } }
            """));

        var result = await catalog.GetByBarcodeAsync(Barcode, TestContext.Current.CancellationToken);

        Assert.Equal("Chocolate", result.Product!.Name);
        Assert.Null(result.Product.Brand);
        Assert.Null(result.Product.SodiumMgPer100g);
    }

    [Fact]
    public async Task Should_Return_NotFound_On_404()
    {
        var (catalog, _) = CreateCatalog(_ => Json("""{ "status": 0 }""", HttpStatusCode.NotFound));

        var result = await catalog.GetByBarcodeAsync(Barcode, TestContext.Current.CancellationToken);

        Assert.Equal(ProductLookupStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Status_Is_Zero()
    {
        var (catalog, _) = CreateCatalog(_ => Json("""{ "status": 0 }"""));

        var result = await catalog.GetByBarcodeAsync(Barcode, TestContext.Current.CancellationToken);

        Assert.Equal(ProductLookupStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Has_No_Name()
    {
        var (catalog, _) = CreateCatalog(_ => Json("""
            { "status": 1, "product": { "product_name_pt": " ", "product_name": null } }
            """));

        var result = await catalog.GetByBarcodeAsync(Barcode, TestContext.Current.CancellationToken);

        Assert.Equal(ProductLookupStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Should_Return_Unavailable_On_Server_Errors(HttpStatusCode statusCode)
    {
        var (catalog, _) = CreateCatalog(_ => new HttpResponseMessage(statusCode));

        var result = await catalog.GetByBarcodeAsync(Barcode, TestContext.Current.CancellationToken);

        Assert.Equal(ProductLookupStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Should_Return_Unavailable_When_Network_Fails()
    {
        var (catalog, _) = CreateCatalog(_ => throw new HttpRequestException("Sem rede"));

        var result = await catalog.GetByBarcodeAsync(Barcode, TestContext.Current.CancellationToken);

        Assert.Equal(ProductLookupStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Should_Return_Unavailable_On_Timeout()
    {
        var (catalog, _) = CreateCatalog(_ => throw new TaskCanceledException("Timeout"));

        var result = await catalog.GetByBarcodeAsync(Barcode, TestContext.Current.CancellationToken);

        Assert.Equal(ProductLookupStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Should_Return_Unavailable_On_Invalid_Json()
    {
        var (catalog, _) = CreateCatalog(_ => Json("<html>Erro</html>"));

        var result = await catalog.GetByBarcodeAsync(Barcode, TestContext.Current.CancellationToken);

        Assert.Equal(ProductLookupStatus.Unavailable, result.Status);
    }
}