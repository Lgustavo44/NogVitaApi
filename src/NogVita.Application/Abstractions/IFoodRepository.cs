using NogVita.Domain.Foods;

namespace NogVita.Application.Abstractions;

public interface IFoodRepository
{
    Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Food?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    void Add(Food food);
}