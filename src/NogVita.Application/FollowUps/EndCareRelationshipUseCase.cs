using NogVita.Application.Abstractions;

namespace NogVita.Application.FollowUps;

public sealed class EndCareRelationshipUseCase(
    ICareRelationshipRepository relationshipRepository,
    IUserRepository userRepository,
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
        relationship.End(currentUserId, timeProvider.GetUtcNow().DateTime);
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