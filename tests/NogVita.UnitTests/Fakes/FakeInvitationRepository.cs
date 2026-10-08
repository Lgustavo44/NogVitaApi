using NogVita.Application.Abstractions;
using NogVita.Domain.Invitations;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeInvitationRepository : IInvitationRepository
{
    public List<NutritionistInvitation> Invitations { get; } = [];

    public Task<NutritionistInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Invitations.FirstOrDefault(i => i.TokenHash == tokenHash));
    }

    public Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        foreach (var invitation in Invitations.Where(i => i.UserId == userId))
            invitation.Revoke(nowUtc);

        return Task.CompletedTask;
    }

    public void Add(NutritionistInvitation invitation)
    {
        Invitations.Add(invitation);
    }
}