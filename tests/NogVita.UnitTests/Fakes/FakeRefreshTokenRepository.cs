using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));
    }

    public void Add(RefreshToken refreshToken)
    {
        Tokens.Add(refreshToken);
    }

    public Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        foreach (var token in Tokens.Where(t => t.UserId == userId))
            token.Revoke(nowUtc);

        return Task.CompletedTask;
    }
}