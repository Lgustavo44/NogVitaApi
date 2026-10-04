using NogVita.Domain.Auth;

namespace NogVita.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    void Add(RefreshToken refreshToken);
    Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
}