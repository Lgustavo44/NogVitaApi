using Microsoft.AspNetCore.Identity;
using NogVita.Application.Abstractions;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Security;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public bool Verify(string passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(null!, passwordHash, password);

        return result is PasswordVerificationResult.Success
                      or PasswordVerificationResult.SuccessRehashNeeded;
    }
}