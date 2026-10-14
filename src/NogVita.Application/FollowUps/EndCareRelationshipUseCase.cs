using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.FollowUps;

public sealed class EndCareRelationshipUseCase(
    ICareRelationshipRepository relationshipRepository,
    IUserRepository userRepository,
    INutritionPlanRepository planRepository,
    FollowUpNotificationService notificationService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <param name="currentUserId">Quem está encerrando (vem do token).</param>
    /// <param name="patientId">O paciente do vínculo: o próprio usuário, ou o paciente escolhido pelo nutricionista.</param>
    public async Task<bool> ExecuteAsync(Guid currentUserId, Guid patientId, CancellationToken cancellationToken = default)
    {
        var relationship = await relationshipRepository.GetActiveForPatientAsync(patientId, cancellationToken);
        if (relationship == null || (currentUserId != relationship.PatientId && currentUserId != relationship.NutritionistId))
        {
            return false;
        }
        relationship.End(currentUserId, timeProvider.GetUtcNow().UtcDateTime);
        var today = BrazilianCalendar.Today(timeProvider);
        var openPlans = await planRepository.ListOpenAsync(relationship.NutritionistId, relationship.PatientId, cancellationToken);

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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var otherUserId = currentUserId == relationship.PatientId ? relationship.NutritionistId : relationship.PatientId;
        var otherUser = await userRepository.GetByIdAsync(otherUserId, cancellationToken);
        var currentUser = await userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (otherUser != null && currentUser != null)
        {
            await notificationService.TrySendEndedAsync(otherUser, currentUser, cancellationToken);
        }

        return true;
    }
}