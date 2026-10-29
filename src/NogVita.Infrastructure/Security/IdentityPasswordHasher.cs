using Microsoft.AspNetCore.Identity;
using NogVita.Application.Abstractions;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Security;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    // Gerado uma vez, com os mesmos parâmetros dos hashes reais, para o custo da verificação ser igual.
    private readonly Lazy<string> _dummyHash;

    public IdentityPasswordHasher()
    {
        _dummyHash = new Lazy<string>(() => _hasher.HashPassword(null!, Guid.NewGuid().ToString()));
    }

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public PasswordCheck Verify(string? passwordHash, string password)
    {
        if (passwordHash is null)
        {
            _hasher.VerifyHashedPassword(null!, _dummyHash.Value, password);
            return PasswordCheck.Failed;
        }

        return _hasher.VerifyHashedPassword(null!, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
            PasswordVerificationResult.Failed => PasswordCheck.Failed
        };
    }
}
