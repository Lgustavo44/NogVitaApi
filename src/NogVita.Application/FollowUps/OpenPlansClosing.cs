using NogVita.Application.Abstractions;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.FollowUps;

internal static class OpenPlansClosing
{
    // Ao fim de um acompanhamento: o plano ativo é finalizado hoje, e os rascunhos são cancelados.
    public static async Task CloseOpenPlansAsync(
        this INutritionPlanRepository planRepository,
        Guid nutritionistId,
        Guid patientId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var openPlans = await planRepository.ListOpenAsync(nutritionistId, patientId, cancellationToken);

        foreach (var plan in openPlans)
        {
            if (plan.Status == NutritionPlanStatus.Active)
            {
                plan.Finish(today);
            }
            else
            {
                plan.Cancel();
            }
        }
    }
}
