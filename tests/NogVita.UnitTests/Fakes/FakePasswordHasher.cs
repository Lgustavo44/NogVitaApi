using NogVita.Application.Abstractions;

namespace NogVita.UnitTests.Fakes;

public sealed class FakePasswordHasher : IPasswordHasher
{
    // Hashes com este prefixo simulam parâmetros antigos (SuccessRehashNeeded).
    public const string OutdatedPrefix = "old:";

    public int VerifyCount { get; private set; }

    public string Hash(string password) => $"hashed:{password}";

    public PasswordCheck Verify(string? passwordHash, string password)
    {
        VerifyCount++;

        if (passwordHash == Hash(password))
        {
            return PasswordCheck.Success;
        }

        if (passwordHash == $"{OutdatedPrefix}{password}")
        {
            return PasswordCheck.SuccessRehashNeeded;
        }

        return PasswordCheck.Failed;
    }
}
