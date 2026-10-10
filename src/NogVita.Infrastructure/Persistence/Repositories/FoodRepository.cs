using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Foods;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class FoodRepository(NogVitaDbContext context) : IFoodRepository
{
    public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Foods.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<Food?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
        context.Foods.FirstOrDefaultAsync(f => f.Barcode == barcode, cancellationToken);

    public void Add(Food food) => context.Foods.Add(food);
}