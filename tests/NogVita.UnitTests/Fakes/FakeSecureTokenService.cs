using NogVita.Application.Abstractions;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeSecureTokenService : ISecureTokenService
{
    public string GenerateToken() => $"refresh-{Guid.NewGuid()}";

    public string Hash(string token) => $"hash:{token}";
}