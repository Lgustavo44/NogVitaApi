using NogVita.Domain.Common;

namespace NogVita.Domain.Invitations;

public class NutritionistInvitation : SingleUseToken
{
    private NutritionistInvitation() { } // EF Core

    public NutritionistInvitation(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime nowUtc)
        : base(userId, tokenHash, expiresAtUtc, nowUtc)
    {
    }
}