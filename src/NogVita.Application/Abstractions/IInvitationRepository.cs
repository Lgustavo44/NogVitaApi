using NogVita.Domain.Invitations;

namespace NogVita.Application.Abstractions;

public interface IInvitationRepository
{
    Task<NutritionistInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    void Add(NutritionistInvitation invitation);
}