using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class PasswordResetTokenRepository(NogVitaDbContext context) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        context.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var pendingTokens = await context.PasswordResetTokens
            .Where(t => t.UserId == userId
                && t.UsedAtUtc == null
                && t.RevokedAtUtc == null
                && t.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var token in pendingTokens)
        {
            token.Revoke(nowUtc);
        }
    }

    public void Add(PasswordResetToken token) => context.PasswordResetTokens.Add(token);
}