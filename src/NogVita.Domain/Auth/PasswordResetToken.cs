using NogVita.Domain.Common;

namespace NogVita.Domain.Auth;

public class PasswordResetToken : SingleUseToken
{
    private PasswordResetToken() { } // EF Core

    public PasswordResetToken(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime nowUtc)
        : base(userId, tokenHash, expiresAtUtc, nowUtc)
    {
    }
}
