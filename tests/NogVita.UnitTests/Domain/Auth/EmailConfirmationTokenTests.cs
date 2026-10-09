using NogVita.Domain.Auth;
using NogVita.Domain.Common;

namespace NogVita.UnitTests.Domain.Auth;

public class EmailConfirmationTokenTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_Be_Valid_Until_Expiration()
    {
        var token = new EmailConfirmationToken(Guid.NewGuid(), "hash", Now.AddHours(24), Now);

        Assert.True(token.IsValid(Now.AddHours(23)));
        Assert.False(token.IsValid(Now.AddHours(24)));
    }

    [Fact]
    public void Should_Not_Be_Used_Twice()
    {
        var token = new EmailConfirmationToken(Guid.NewGuid(), "hash", Now.AddHours(24), Now);
        token.MarkAsUsed(Now.AddHours(1));

        Assert.Throws<DomainException>(() => token.MarkAsUsed(Now.AddHours(2)));
    }
}