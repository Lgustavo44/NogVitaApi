using NogVita.Application.Abstractions;

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
    TimeProvider timeProvider)
{
    public async Task<ResendInvitationStatus> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user?.NutritionistProfile is null)
            return ResendInvitationStatus.NotFound;

        if (user.NutritionistProfile.IsActive)
            return ResendInvitationStatus.AlreadyActive;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await invitationRepository.RevokePendingForUserAsync(user.Id, now, cancellationToken);

        var token = invitationService.CreateInvitation(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var emailSent = await invitationService.TrySendAsync(user, token, cancellationToken);

        return emailSent ? ResendInvitationStatus.Sent : ResendInvitationStatus.EmailFailed;
    }
}