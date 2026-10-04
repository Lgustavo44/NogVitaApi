using NogVita.Application.Abstractions;

namespace NogVita.UnitTests.Fakes;

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string passwordHash, string password) => passwordHash == Hash(password);
}