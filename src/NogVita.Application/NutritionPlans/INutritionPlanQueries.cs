using NogVita.Application.Common.Pagination;

namespace NogVita.Application.NutritionPlans;

public interface INutritionPlanQueries
{
    Task<PagedResponse<NutritionPlanResponse>> ListByNutritionistAndPatientAsync(
        Guid nutritionistId, Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default);

    Task<NutritionPlanResponse?> GetByNutritionistAsync(
        Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default);

    Task<PagedResponse<NutritionPlanResponse>> ListByPatientAsync(
        Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default);

    Task<NutritionPlanResponse?> GetByPatientAsync(
        Guid patientId, Guid planId, CancellationToken cancellationToken = default);

    Task<NutritionPlanResponse?> GetCurrentByPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default);
}