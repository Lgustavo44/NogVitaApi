using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.FollowUps;

namespace NogVita.Application.Admin;

public enum DeactivateUserResult
{
    Success,
    NotFound,
    CannotDeactivateSelf
}

public sealed class DeactivateUserUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICareRelationshipRepository relationshipRepository,
    INutritionistRequestRepository requestRepository,
    INutritionPlanRepository planRepository,
    FollowUpNotificationService notificationService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<DeactivateUserResult> ExecuteAsync(Guid adminId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (adminId == userId)
            return DeactivateUserResult.CannotDeactivateSelf;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            return DeactivateUserResult.NotFound;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        user.Deactivate();
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, now, cancellationToken);

        // Um nutricionista desativado não atende mais: os pacientes ficam livres para escolher outro.
        var endedPatientIds = user.NutritionistProfile is null
            ? []
            : await ReleasePatientsAsync(adminId, user.Id, now, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var patientId in endedPatientIds)
        {
            var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);
            if (patient is not null)
            {
                await notificationService.TrySendEndedAsync(patient, user, cancellationToken);
            }
        }

        return DeactivateUserResult.Success;
    }

    private async Task<List<Guid>> ReleasePatientsAsync(Guid adminId, Guid nutritionistId, DateTime now, CancellationToken cancellationToken)
    {
        var today = BrazilianCalendar.Today(timeProvider);
        var relationships = await relationshipRepository.ListActiveForNutritionistAsync(nutritionistId, cancellationToken);

        foreach (var relationship in relationships)
        {
            relationship.EndByAdmin(adminId, now);
            await planRepository.CloseOpenPlansAsync(nutritionistId, relationship.PatientId, today, cancellationToken);
        }

        // Solicitações pendentes seriam respondidas por ninguém e prenderiam o paciente (uma pendente por vez).
        foreach (var request in await requestRepository.ListPendingForNutritionistAsync(nutritionistId, cancellationToken))
        {
            request.Reject(now);
        }

        return relationships.Select(r => r.PatientId).ToList();
    }
}
