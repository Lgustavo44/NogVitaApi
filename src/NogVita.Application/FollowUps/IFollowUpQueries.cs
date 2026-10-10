using NogVita.Application.Common.Pagination;

namespace NogVita.Application.FollowUps;

public interface IFollowUpQueries
{
    Task<PagedResponse<PatientRequestItem>> ListPatientRequestsAsync(
        Guid patientId, NutritionistRequestListRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<NutritionistRequestItem>> ListNutritionistRequestsAsync(
        Guid nutritionistId, NutritionistRequestListRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<MyPatientItem>> ListMyPatientsAsync(
        Guid nutritionistId, MyPatientsListRequest request, CancellationToken cancellationToken = default);
}