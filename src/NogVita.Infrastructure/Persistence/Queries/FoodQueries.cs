using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Foods;
using NogVita.Domain.Foods;
using NogVita.Infrastructure.Queries;

namespace NogVita.Infrastructure.Persistence.Queries;

internal sealed class FoodQueries(NogVitaDbContext context) : IFoodQueries
{
    public Task<PagedResponse<FoodResponse>> SearchAsync(FoodSearchRequest request, CancellationToken cancellationToken = default)
    {
        var pattern = $"%{QueryableExtensions.EscapeLikePattern(request.Search.Trim())}%";

        var query = context.Foods
            .AsNoTracking()
            .Where(f => f.IsActive)
            .Where(f => EF.Functions.ILike(EF.Functions.Unaccent(f.Name), EF.Functions.Unaccent(pattern), @"\"));

        if (request.Source is not null)
        {
            query = query.Where(f => f.Source == request.Source);
        }

        return query
            .OrderBy(f => f.Name)
            .ThenBy(f => f.Id)
            .Select(ToResponse())
            .ToPagedResponseAsync(request, cancellationToken);
    }

    public Task<FoodResponse?> GetByIdAsync(Guid id, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = context.Foods
            .AsNoTracking()
            .Where(f => f.Id == id);

        if (!includeInactive)
        {
            query = query.Where(f => f.IsActive);
        }

        return query
            .Select(ToResponse())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<Food, FoodResponse>> ToResponse() =>
        f => new FoodResponse(
            f.Id,
            f.Name,
            f.Brand,
            f.Barcode,
            f.Category,
            f.Source,
            f.SourceReference,
            f.IsActive,
            f.Nutrients.EnergyKcal,
            f.Nutrients.Protein,
            f.Nutrients.Carbohydrate,
            f.Nutrients.Fat,
            f.Nutrients.Fiber,
            f.Nutrients.SodiumMg);
}