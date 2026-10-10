using NogVita.Application.Abstractions;
using NogVita.Domain.Foods;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeFoodRepository : IFoodRepository
{
    public List<Food> Foods { get; } = [];

    public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Foods.FirstOrDefault(f => f.Id == id));

    public Task<Food?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
        Task.FromResult(Foods.FirstOrDefault(f => f.Barcode == barcode));

    public void Add(Food food) => Foods.Add(food);
}