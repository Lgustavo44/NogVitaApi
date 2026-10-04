using NogVita.Domain.Auth;
using NogVita.Domain.Common;

namespace NogVita.UnitTests.Domain.Auth;

public class RefreshTokenTests
{
    private const string ValidHash = "hash-do-token";
    private static readonly Guid UserId = Guid.NewGuid();

    private static RefreshToken CreateToken(DateTime expiresAtUtc) => new(UserId, ValidHash, expiresAtUtc);

    [Fact]
    public void Should_Be_Active_When_Not_Revoked_And_Not_Expired()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = CreateToken(expiresAt);

        var isActive = token.IsActive(expiresAt.AddMinutes(-1));

        Assert.True(isActive);
    }

    [Fact]
    public void Should_Not_Be_Active_When_Expired()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = CreateToken(expiresAt);

        var isActive = token.IsActive(expiresAt.AddSeconds(1));

        Assert.False(isActive);
    }

    [Fact]
    public void Should_Not_Be_Active_Exactly_At_Expiration()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = CreateToken(expiresAt);

        var isActive = token.IsActive(expiresAt);

        Assert.False(isActive);
    }

    [Fact]
    public void Should_Not_Be_Active_When_Revoked()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = CreateToken(expiresAt);
        var now = expiresAt.AddDays(-6);

        token.Revoke(now);

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive(now));
    }

    [Fact]
    public void Should_Keep_First_Revocation_Date()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = CreateToken(expiresAt);
        var firstRevocation = expiresAt.AddDays(-6);
        var secondRevocation = firstRevocation.AddHours(1);

        token.Revoke(firstRevocation);
        token.Revoke(secondRevocation);

        Assert.Equal(firstRevocation, token.RevokedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Not_Create_Without_Hash(string tokenHash)
    {
        Assert.Throws<DomainException>(() => new RefreshToken(UserId, tokenHash, DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public void Should_Not_Create_With_Past_Expiration()
    {
        Assert.Throws<DomainException>(() => new RefreshToken(UserId, ValidHash, DateTime.UtcNow.AddMinutes(-1)));
    }
}