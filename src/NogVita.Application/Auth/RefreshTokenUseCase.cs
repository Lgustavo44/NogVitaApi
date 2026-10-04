using NogVita.Application.Abstractions;

namespace NogVita.Application.Auth;

public sealed class RefreshTokenUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    ISecureTokenService secureTokenService,
    TokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AuthResponse?> ExecuteAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = secureTokenService.Hash(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (storedToken is null)
            return null;

        if (storedToken.IsRevoked)
        {
            await RevokeAllSessionsAsync(storedToken.UserId, now, cancellationToken);
            return null;
        }

        if (storedToken.IsExpired(now))
            return null;

        var user = await userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            await RevokeAllSessionsAsync(storedToken.UserId, now, cancellationToken);
            return null;
        }

        storedToken.Revoke(now);
        var response = tokenIssuer.Issue(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    private async Task RevokeAllSessionsAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        await refreshTokenRepository.RevokeAllForUserAsync(userId, nowUtc, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}