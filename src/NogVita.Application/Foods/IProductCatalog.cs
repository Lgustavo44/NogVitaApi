namespace NogVita.Application.Foods;

public enum ProductLookupStatus
{
    Found,
    NotFound,
    Unavailable
}

public sealed record ProductLookupResult(ProductLookupStatus Status, ExternalProduct? Product = null);

public interface IProductCatalog
{
    Task<ProductLookupResult> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
}