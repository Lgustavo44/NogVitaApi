using NogVita.Application.Abstractions;

namespace NogVita.Application.Auth;

public sealed class LogoutUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    ISecureTokenService secureTokenService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = secureTokenService.Hash(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked)
            return;

        storedToken.Revoke(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}