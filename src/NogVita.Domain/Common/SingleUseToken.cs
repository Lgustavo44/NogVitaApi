namespace NogVita.Domain.Common;

public abstract class SingleUseToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    protected SingleUseToken() { } // EF Core

    protected SingleUseToken(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("O hash do token é obrigatório.");

        if (expiresAtUtc <= nowUtc)
            throw new DomainException("A data de expiração precisa estar no futuro.");

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
            throw new DomainException("Token inválido ou expirado.");

        UsedAtUtc = nowUtc;
    }

    public void Revoke(DateTime nowUtc)
    {
        if (IsUsed || IsRevoked)
            return;

        RevokedAtUtc = nowUtc;
    }
}