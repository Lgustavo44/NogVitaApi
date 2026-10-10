using NogVita.Application.Abstractions;
using NogVita.Domain.Foods;

namespace NogVita.Application.Foods;

public enum LookupProductStatus
{
    Found,
    InvalidBarcode,
    NotFound,
    Unavailable
}

public sealed record LookupProductResult(LookupProductStatus Status, ProductPreviewResponse? Preview = null);

public sealed class LookupProductByBarcodeUseCase(IFoodRepository foodRepository, IProductCatalog productCatalog)
{
    public async Task<LookupProductResult> ExecuteAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var normalizedBarcode = barcode?.Trim() ?? string.Empty;

        if (!Barcode.IsValid(normalizedBarcode))
        {
            return new LookupProductResult(LookupProductStatus.InvalidBarcode);
        }

        var localFood = await foodRepository.GetByBarcodeAsync(normalizedBarcode, cancellationToken);

        if (localFood is not null)
        {
            return localFood.IsActive
                ? new LookupProductResult(LookupProductStatus.Found, ProductPreviewResponse.FromFood(localFood))
                : new LookupProductResult(LookupProductStatus.NotFound);
        }

        var result = await productCatalog.GetByBarcodeAsync(normalizedBarcode, cancellationToken);

        return result.Status switch
        {
            ProductLookupStatus.Found =>
                new LookupProductResult(LookupProductStatus.Found, ProductPreviewResponse.FromExternal(result.Product!)),
            ProductLookupStatus.NotFound => new LookupProductResult(LookupProductStatus.NotFound),
            _ => new LookupProductResult(LookupProductStatus.Unavailable)
        };
    }
}