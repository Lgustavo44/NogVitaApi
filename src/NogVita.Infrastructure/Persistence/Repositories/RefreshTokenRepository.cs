using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;

namespace NogVita.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(NogVitaDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public void Add(RefreshToken refreshToken)
    {
        context.RefreshTokens.Add(refreshToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var activeTokens = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.Revoke(nowUtc);
    }
}