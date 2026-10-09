using NogVita.Domain.Common;

namespace NogVita.Domain.Auth;

public class EmailConfirmationToken : SingleUseToken
{
    private EmailConfirmationToken() { } // EF Core

    public EmailConfirmationToken(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime nowUtc)
        : base(userId, tokenHash, expiresAtUtc, nowUtc)
    {
    }
}