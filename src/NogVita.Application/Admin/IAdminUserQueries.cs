using NogVita.Application.Common.Pagination;

namespace NogVita.Application.Admin;

public interface IAdminUserQueries
{
    Task<PagedResponse<AdminUserSummary>> ListUsersAsync(AdminUserListRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserDetail?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<AdminPatientSummary>> ListPatientsAsync(AdminUserListRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<AdminNutritionistSummary>> ListNutritionistsAsync(AdminUserListRequest request, CancellationToken cancellationToken = default);
}