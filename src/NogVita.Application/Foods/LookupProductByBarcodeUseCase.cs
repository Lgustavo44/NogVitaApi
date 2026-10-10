namespace NogVita.Application.Foods;

public enum LookupProductStatus
{
    Found,
    InvalidBarcode,
    NotFound,
    Unavailable
}

public sealed record LookupProductResult(LookupProductStatus Status, ExternalProduct? Product = null);

public sealed class LookupProductByBarcodeUseCase(IProductCatalog productCatalog)
{
    public async Task<LookupProductResult> ExecuteAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var normalizedBarcode = barcode?.Trim() ?? string.Empty;

        if (!Barcode.IsValid(normalizedBarcode))
        {
            return new LookupProductResult(LookupProductStatus.InvalidBarcode);
        }

        var result = await productCatalog.GetByBarcodeAsync(normalizedBarcode, cancellationToken);

        return result.Status switch
        {
            ProductLookupStatus.Found => new LookupProductResult(LookupProductStatus.Found, result.Product),
            ProductLookupStatus.NotFound => new LookupProductResult(LookupProductStatus.NotFound),
            _ => new LookupProductResult(LookupProductStatus.Unavailable)
        };
    }
}