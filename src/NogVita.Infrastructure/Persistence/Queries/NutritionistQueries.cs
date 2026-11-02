using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Nutritionists;
using NogVita.Application.Photos;

namespace NogVita.Infrastructure.Persistence.Queries;

internal sealed class NutritionistQueries(NogVitaDbContext context, IFileStorage fileStorage) : INutritionistQueries
{
    public async Task<PagedResponse<NutritionistListItem>> ListAsync(NutritionistListRequest request, CancellationToken cancellationToken = default)
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

        var page = await query
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Select(u => new
            {
                // O PhotoUrl vai null aqui: uma árvore de expressão não aceita argumento opcional omitido (CS0854).
                Item = new NutritionistListItem(
                    u.Id,
                    u.Name,
                    u.NutritionistProfile!.CrnRegion,
                    u.NutritionistProfile.CrnNumber,
                    u.NutritionistProfile.Bio,
                    null),
                u.NutritionistProfile.PhotoPath
            })
            .ToPagedResponseAsync(request, cancellationToken);

        // A foto do nutricionista é pública: a URL é montada aqui, sem chamar o Supabase.
        return page.Map(i => i.Item with { PhotoUrl = fileStorage.NutritionistPhotoUrl(i.PhotoPath) });
    }
}
