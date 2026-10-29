using NogVita.Application.Abstractions;
using NogVita.Application.Admin;
using NogVita.Domain.Audit;

namespace NogVita.Application.Nutritionists;

public enum ResendInvitationStatus
{
    Sent,
    EmailFailed,
    NotFound,
    AlreadyActive
}

public sealed class ResendNutritionistInvitationUseCase(
    IUserRepository userRepository,
    IInvitationRepository invitationRepository,
    IUnitOfWork unitOfWork,
    NutritionistInvitationService invitationService,
    TimeProvider timeProvider,
    AdminAuditLog auditLog)
{
    public async Task<ResendInvitationStatus> ExecuteAsync(Guid adminId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user?.NutritionistProfile is null)
        {
            return ResendInvitationStatus.NotFound;
        }

        if (user.NutritionistProfile.IsActive)
        {
            return ResendInvitationStatus.AlreadyActive;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await invitationRepository.RevokePendingForUserAsync(user.Id, now, cancellationToken);

        var token = invitationService.CreateInvitation(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var emailSent = await invitationService.TrySendAsync(user, token, cancellationToken);
        auditLog.Record(adminId, AdminAuditAction.NutritionistInvitationResent, user.Id, emailSent ? null : "O e-mail do convite falhou.");

        return emailSent ? ResendInvitationStatus.Sent : ResendInvitationStatus.EmailFailed;
    }
}
