using NogVita.Application.Common.Pagination;

namespace NogVita.Application.Admin;

public interface IAdminUserQueries
{
    Task<PagedResponse<AdminUserSummary>> ListUsersAsync(AdminUserListRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserDetail?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<AdminPatientSummary>> ListPatientsAsync(AdminUserListRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<AdminNutritionistSummary>> ListNutritionistsAsync(AdminUserListRequest request, CancellationToken cancellationToken = default);
    Task<AdminOverviewResponse> GetOverviewAsync(DateOnly diarySince, CancellationToken cancellationToken = default);
}

// O resumo do início do admin: o uso do diário e o que ainda depende de alguém.
public sealed record AdminOverviewResponse(
    int PatientsPendingConsent,
    int DiaryDaysLast7Days,
    int WeeklyCheckInsAwaitingFeedback,
    int ActiveFoods,
    int ActiveFoodsWithPortions);
