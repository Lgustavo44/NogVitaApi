using NogVita.Application.Abstractions;

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

        user.Deactivate();
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DeactivateUserResult.Success;
    }
}