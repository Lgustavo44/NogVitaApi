using NogVita.Application.Abstractions;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public AccessToken Generate(User user) => new($"token-for-{user.Id}", new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
}