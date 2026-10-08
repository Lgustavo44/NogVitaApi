using NogVita.Application.Abstractions;

namespace NogVita.Application.Nutritionists;

public enum AcceptInvitationResult
{
    Accepted,
    InvalidInvitation,
    PasswordRequired
}

public sealed class AcceptNutritionistInvitationUseCase(
    IInvitationRepository invitationRepository,
    IUserRepository userRepository,
    ISecureTokenService secureTokenService,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AcceptInvitationResult> ExecuteAsync(AcceptInvitationRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var invitation = await invitationRepository.GetByTokenHashAsync(secureTokenService.Hash(request.Token), cancellationToken);

        if (invitation is null || !invitation.IsValid(now))
            return AcceptInvitationResult.InvalidInvitation;

        var user = await userRepository.GetByIdAsync(invitation.UserId, cancellationToken);

        if (user?.NutritionistProfile is null)
            return AcceptInvitationResult.InvalidInvitation;

        if (user.PasswordHash is null)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                return AcceptInvitationResult.PasswordRequired;

            user.SetPasswordHash(passwordHasher.Hash(request.Password));
            user.Activate();
        }

        invitation.MarkAsUsed(now);
        user.NutritionistProfile.Activate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AcceptInvitationResult.Accepted;
    }
}