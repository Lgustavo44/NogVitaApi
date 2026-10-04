using NogVita.Domain.Common;

namespace NogVita.Domain.Auth;

public class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    private RefreshToken() { } // EF Core

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("O hash do token é obrigatório.");

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new DomainException("A data de expiração precisa estar no futuro.");

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    public void Revoke(DateTime nowUtc)
    {
        if (IsRevoked)
            return;

        RevokedAtUtc = nowUtc;
    }
}