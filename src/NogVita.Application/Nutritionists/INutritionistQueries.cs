using NogVita.Application.Common.Pagination;

namespace NogVita.Application.Nutritionists;

public interface INutritionistQueries
{
    Task<PagedResponse<NutritionistListItem>> ListAsync(NutritionistListRequest request, CancellationToken cancellationToken = default);
}