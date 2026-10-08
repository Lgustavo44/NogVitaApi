using NogVita.Domain.Common;

namespace NogVita.Domain.Invitations;

public class NutritionistInvitation : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    private NutritionistInvitation() { } // EF Core

    public NutritionistInvitation(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("O hash do token é obrigatório.");
        if (expiresAtUtc <= nowUtc)
            throw new DomainException("A data de expiração deve ser posterior à data atual.");
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsUsed => UsedAtUtc is not null;

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsValid(DateTime nowUtc) => !IsUsed && !IsRevoked && !IsExpired(nowUtc);

    public void MarkAsUsed(DateTime nowUtc)
    {
        if (!IsValid(nowUtc))
            throw new DomainException("Convite inválido ou expirado.");

        UsedAtUtc = nowUtc;
    }

    public void Revoke(DateTime nowUtc)
    {
        if (IsUsed || IsRevoked)
            return;

        RevokedAtUtc = nowUtc;
    }
}