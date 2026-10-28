using NogVita.Application.Common.Pagination;

namespace NogVita.Application.Foods;

public interface IFoodQueries
{
    /// <param name="includeInactive">Só a busca do admin passa <c>true</c>: os inativos têm dados errados e não podem ir para um plano.</param>
    Task<PagedResponse<FoodResponse>> SearchAsync(
        FoodSearchRequest request,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<FoodResponse?> GetByIdAsync(Guid id, bool includeInactive, CancellationToken cancellationToken = default);
}