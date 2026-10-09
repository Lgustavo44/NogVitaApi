using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Nutritionists;
using NogVita.Infrastructure.Queries;

namespace NogVita.Infrastructure.Persistence.Queries;

internal sealed class NutritionistQueries(NogVitaDbContext context) : INutritionistQueries
{
    public Task<PagedResponse<NutritionistListItem>> ListAsync(NutritionistListRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.NutritionistProfile != null && u.NutritionistProfile.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{QueryableExtensions.EscapeLikePattern(request.Search.Trim())}%";
            query = query.Where(u => EF.Functions.ILike(u.Name, pattern, @"\"));
        }

        if (request.CrnRegion is not null)
        {
            query = query.Where(u => u.NutritionistProfile!.CrnRegion == request.CrnRegion);
        }

        return query
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Select(u => new NutritionistListItem(
                u.Id,
                u.Name,
                u.NutritionistProfile!.CrnRegion,
                u.NutritionistProfile.CrnNumber,
                u.NutritionistProfile.Bio))
            .ToPagedResponseAsync(request, cancellationToken);
    }
}