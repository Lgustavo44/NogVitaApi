using NogVita.Domain.Common;
using NogVita.Domain.Invitations;

namespace NogVita.UnitTests.Domain.Invitations;

public class NutritionistInvitationTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ExpiresAt = Now.AddHours(72);

    private static NutritionistInvitation CreateInvitation() =>
        new(Guid.NewGuid(), "hash-do-convite", ExpiresAt, Now);

    [Fact]
    public void Should_Be_Valid_When_New()
    {
        var invitation = CreateInvitation();

        Assert.True(invitation.IsValid(Now.AddHours(1)));
    }

    [Fact]
    public void Should_Be_Invalid_Exactly_At_Expiration()
    {
        var invitation = CreateInvitation();

        Assert.False(invitation.IsValid(ExpiresAt));
    }

    [Fact]
    public void Should_Mark_As_Used()
    {
        var invitation = CreateInvitation();
        var usedAt = Now.AddHours(2);

        invitation.MarkAsUsed(usedAt);

        Assert.True(invitation.IsUsed);
        Assert.Equal(usedAt, invitation.UsedAtUtc);
        Assert.False(invitation.IsValid(usedAt));
    }

    [Fact]
    public void Should_Not_Be_Used_Twice()
    {
        var invitation = CreateInvitation();
        invitation.MarkAsUsed(Now.AddHours(1));

        Assert.Throws<DomainException>(() => invitation.MarkAsUsed(Now.AddHours(2)));
    }

    [Fact]
    public void Should_Not_Be_Used_When_Expired()
    {
        var invitation = CreateInvitation();

        Assert.Throws<DomainException>(() => invitation.MarkAsUsed(ExpiresAt.AddMinutes(1)));
    }

    [Fact]
    public void Should_Not_Be_Used_When_Revoked()
    {
        var invitation = CreateInvitation();
        invitation.Revoke(Now.AddHours(1));

        Assert.Throws<DomainException>(() => invitation.MarkAsUsed(Now.AddHours(2)));
    }

    [Fact]
    public void Should_Not_Revoke_Used_Invitation()
    {
        var invitation = CreateInvitation();
        invitation.MarkAsUsed(Now.AddHours(1));

        invitation.Revoke(Now.AddHours(2));

        Assert.False(invitation.IsRevoked);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Not_Create_Without_Hash(string tokenHash)
    {
        Assert.Throws<DomainException>(() => new NutritionistInvitation(Guid.NewGuid(), tokenHash, ExpiresAt, Now));
    }

    [Fact]
    public void Should_Not_Create_With_Expiration_In_The_Past()
    {
        Assert.Throws<DomainException>(() => new NutritionistInvitation(Guid.NewGuid(), "hash", Now.AddMinutes(-1), Now));
    }
}