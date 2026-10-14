using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;

namespace NogVita.Application.NutritionPlans;

public sealed class NutritionistPlanQueriesUseCase(
    CareAccessService careAccessService,
    INutritionPlanQueries planQueries)
{
    public async Task<PagedResponse<NutritionPlanResponse>?> ListForPatientAsync(
        Guid nutritionistId,
        Guid patientId,
        NutritionPlanListRequest request,
        CancellationToken cancellationToken = default)
    {
        var followsPatient = await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, patientId, cancellationToken);

        return followsPatient
            ? await planQueries.ListByNutritionistAndPatientAsync(nutritionistId, patientId, request, cancellationToken)
            : null;
    }

    public async Task<NutritionPlanDetailResponse?> GetAsync(
        Guid nutritionistId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var plan = await planQueries.GetByNutritionistAsync(nutritionistId, planId, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        var followsPatient = await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, plan.Plan.PatientId, cancellationToken);

        return followsPatient ? plan : null;
    }
}