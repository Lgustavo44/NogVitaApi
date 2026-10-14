using NogVita.Application.Common.Pagination;

namespace NogVita.Application.NutritionPlans;

public interface INutritionPlanQueries
{
    Task<PagedResponse<NutritionPlanResponse>> ListByNutritionistAndPatientAsync(
        Guid nutritionistId, Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<NutritionPlanResponse>> ListByPatientAsync(
        Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default);

    Task<NutritionPlanDetailResponse?> GetByNutritionistAsync(
        Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default);

    Task<NutritionPlanDetailResponse?> GetByPatientAsync(
        Guid patientId, Guid planId, CancellationToken cancellationToken = default);

    Task<NutritionPlanDetailResponse?> GetCurrentByPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default);
}