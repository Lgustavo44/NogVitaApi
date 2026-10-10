using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NogVita.Application.Foods;

namespace NogVita.Infrastructure.Foods.OpenFoodFacts;

internal sealed class OpenFoodFactsProductCatalog(
    HttpClient httpClient,
    ILogger<OpenFoodFactsProductCatalog> logger) : IProductCatalog
{
    private const string Fields = "product_name,product_name_pt,brands,nutriments";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<ProductLookupResult> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync($"api/v2/product/{barcode}?fields={Fields}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ProductLookupResult(ProductLookupStatus.NotFound);
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Open Food Facts respondeu {StatusCode} para o código de barras {Barcode}.",
                    (int)response.StatusCode,
                    barcode);
                return new ProductLookupResult(ProductLookupStatus.Unavailable);
            }

            var body = await response.Content.ReadFromJsonAsync<OpenFoodFactsResponse>(JsonOptions, cancellationToken);

            if (body is null || body.Status != 1 || body.Product is null)
            {
                return new ProductLookupResult(ProductLookupStatus.NotFound);
            }
            // TODO 2: escolher o nome: o ProductNamePt, se tiver texto; senão o ProductName
            //         (use string.IsNullOrWhiteSpace e Trim)
            //         se os dois estiverem vazios → NotFound (um produto sem nome não serve para um plano)
            var productName = !string.IsNullOrWhiteSpace(body.Product.ProductNamePt)
                ? body.Product.ProductNamePt.Trim()
                : body.Product.ProductName?.Trim();

            if (string.IsNullOrWhiteSpace(productName))
            {
                return new ProductLookupResult(ProductLookupStatus.NotFound);
            }

            var nutriments = body.Product.Nutriments;

            var brand = body.Product.Brands?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            var externalProduct = new ExternalProduct(
                Barcode: barcode,
                Name: productName,
                Brand: brand,
                EnergyKcalPer100g: nutriments?.EnergyKcal,
                ProteinPer100g: nutriments?.Proteins,
                CarbohydratePer100g: nutriments?.Carbohydrates,
                FatPer100g: nutriments?.Fat,
                FiberPer100g: nutriments?.Fiber,
                SodiumMgPer100g: nutriments?.SodiumGrams * 1000m);

            return new ProductLookupResult(ProductLookupStatus.Found, externalProduct);


        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Falha de rede ao consultar o Open Food Facts para o código {Barcode}.", barcode);
            return new ProductLookupResult(ProductLookupStatus.Unavailable);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Tempo esgotado ao consultar o Open Food Facts para o código {Barcode}.", barcode);
            return new ProductLookupResult(ProductLookupStatus.Unavailable);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Resposta inválida do Open Food Facts para o código {Barcode}.", barcode);
            return new ProductLookupResult(ProductLookupStatus.Unavailable);
        }
    }
}