using NogVita.Application.Abstractions;
using NogVita.Domain.Common;
using NogVita.Domain.Foods;

namespace NogVita.Application.Foods;

public enum ImportProductStatus
{
    Imported,
    AlreadyImported,
    InvalidBarcode,
    NotFound,
    Unavailable,
    InvalidData
}

public sealed record ImportProductResult(ImportProductStatus Status, FoodResponse? Food = null);

public sealed class ImportProductUseCase(
    IFoodRepository foodRepository,
    IProductCatalog productCatalog,
    IUnitOfWork unitOfWork)
{
    public async Task<ImportProductResult> ExecuteAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var normalizedBarcode = barcode?.Trim() ?? string.Empty;

        if (!Barcode.IsValid(normalizedBarcode))
        {
            return new ImportProductResult(ImportProductStatus.InvalidBarcode);
        }

        var existingFood = await foodRepository.GetByBarcodeAsync(normalizedBarcode, cancellationToken);

        if (existingFood is not null)
        {
            // Inativo: o admin tirou o alimento de uso, e importar de novo não deve trazê-lo de volta.
            return existingFood.IsActive
                ? new ImportProductResult(ImportProductStatus.AlreadyImported, FoodResponse.From(existingFood))
                : new ImportProductResult(ImportProductStatus.NotFound);
        }

        var result = await productCatalog.GetByBarcodeAsync(normalizedBarcode, cancellationToken);

        switch (result.Status)
        {
            case ProductLookupStatus.Found:
                break;
            case ProductLookupStatus.NotFound:
                return new ImportProductResult(ImportProductStatus.NotFound);
            default:
                return new ImportProductResult(ImportProductStatus.Unavailable);
        }

        var product = result.Product!;

        Food food;

        try
        {
            var nutrients = new NutrientsPer100g(
                product.EnergyKcalPer100g,
                product.ProteinPer100g,
                product.CarbohydratePer100g,
                product.FatPer100g,
                product.FiberPer100g,
                product.SodiumMgPer100g);

            food = Food.FromOpenFoodFacts(product.Barcode, product.Name, product.Brand, nutrients);
        }
        catch (DomainException)
        {
            return new ImportProductResult(ImportProductStatus.InvalidData);
        }

        foodRepository.Add(food);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportProductResult(ImportProductStatus.Imported, FoodResponse.From(food));
    }
}