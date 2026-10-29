using NogVita.Application.Abstractions;
using NogVita.Application.FollowUps;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

public sealed class NutritionistPlanAccess(
    CareAccessService careAccessService,
    INutritionPlanRepository planRepository)
{
    public async Task<NutritionPlan?> LoadOwnPlanAsync(
        Guid nutritionistId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var plan = await planRepository.GetByIdAsync(planId, cancellationToken);

        if (plan is null || plan.NutritionistId != nutritionistId)
        {
            return null;
        }

        var followsPatient = await careAccessService.NutritionistFollowsPatientAsync(
            nutritionistId, plan.PatientId, cancellationToken);

        return followsPatient ? plan : null;
    }
}
