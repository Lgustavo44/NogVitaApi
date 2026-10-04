using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;
using NogVita.Domain.Users;

namespace NogVita.Application.Auth;

public sealed class TokenIssuer(
    IJwtTokenGenerator jwtTokenGenerator,
    ISecureTokenService secureTokenService,
    IRefreshTokenRepository refreshTokenRepository,
    RefreshTokenSettings settings,
    TimeProvider timeProvider)
{
    public AuthResponse Issue(User user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var accessToken = jwtTokenGenerator.Generate(user);

        var refreshTokenValue = secureTokenService.GenerateToken();
        var refreshToken = new RefreshToken(
            user.Id,
            secureTokenService.Hash(refreshTokenValue),
            now.AddDays(settings.ExpirationDays));

        refreshTokenRepository.Add(refreshToken);

        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshTokenValue,
            refreshToken.ExpiresAtUtc);
    }
}