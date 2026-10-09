using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;
using NogVita.Domain.Invitations;

namespace NogVita.Infrastructure.Persistence.Repositories;

public sealed class EmailConfirmationTokenRepository(NogVitaDbContext context) : IEmailConfirmationTokenRepository
{
    public Task<EmailConfirmationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return context.EmailConfirmationTokens.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var pending = await context.EmailConfirmationTokens
            .Where(i => i.UserId == userId && i.UsedAtUtc == null && i.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in pending)
            token.Revoke(nowUtc);
    }

    public void Add(EmailConfirmationToken token)
    {
        context.EmailConfirmationTokens.Add(token);
    }
}