using NogVita.Application.Foods;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeProductCatalog : IProductCatalog
{
    public Dictionary<string, ExternalProduct> Products { get; } = [];
    public bool IsUnavailable { get; set; }
    public int CallCount { get; private set; }

    public Task<ProductLookupResult> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        CallCount++;

        if (IsUnavailable)
        {
            return Task.FromResult(new ProductLookupResult(ProductLookupStatus.Unavailable));
        }

        return Task.FromResult(Products.TryGetValue(barcode, out var product)
            ? new ProductLookupResult(ProductLookupStatus.Found, product)
            : new ProductLookupResult(ProductLookupStatus.NotFound));
    }
}