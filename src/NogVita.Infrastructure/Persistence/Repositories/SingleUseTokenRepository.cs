using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Common;

namespace NogVita.Infrastructure.Persistence.Repositories;

// Fase 28: a implementação comum dos três repositórios de tokens de uso único.
// revokeExpired diz se a revogação também carimba os tokens já expirados. Os três repositórios guardavam essa
// diferença antes da refatoração, e ela continua igual: só a redefinição de senha deixa os expirados como estão.
public abstract class SingleUseTokenRepository<TToken>(NogVitaDbContext context, bool revokeExpired)
    : ISingleUseTokenRepository<TToken> where TToken : SingleUseToken
{
    private DbSet<TToken> Tokens => context.Set<TToken>();

    public Task<TToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Tokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var pending = Tokens.Where(t => t.UserId == userId && t.UsedAtUtc == null && t.RevokedAtUtc == null);

        if (!revokeExpired)
        {
            pending = pending.Where(t => t.ExpiresAtUtc > nowUtc);
        }

        foreach (var token in await pending.ToListAsync(cancellationToken))
        {
            token.Revoke(nowUtc);
        }
    }

    public void Add(TToken token) => Tokens.Add(token);
}
