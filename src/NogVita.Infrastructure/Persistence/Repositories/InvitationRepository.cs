using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Invitations;

namespace NogVita.Infrastructure.Persistence.Repositories;

public sealed class InvitationRepository(NogVitaDbContext context) : IInvitationRepository
{
    public Task<NutritionistInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return context.NutritionistInvitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var pending = await context.NutritionistInvitations
            .Where(i => i.UserId == userId && i.UsedAtUtc == null && i.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var invitation in pending)
            invitation.Revoke(nowUtc);
    }

    public void Add(NutritionistInvitation invitation)
    {
        context.NutritionistInvitations.Add(invitation);
    }
}