using NogVita.Application.Common.Pagination;

namespace NogVita.Application.Foods;

public interface IFoodQueries
{
    Task<PagedResponse<FoodResponse>> SearchAsync(FoodSearchRequest request, CancellationToken cancellationToken = default);

    Task<FoodResponse?> GetByIdAsync(Guid id, bool includeInactive, CancellationToken cancellationToken = default);
}