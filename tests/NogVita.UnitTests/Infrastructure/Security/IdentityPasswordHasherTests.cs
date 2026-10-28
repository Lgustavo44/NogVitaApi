using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NogVita.Application.Abstractions;
using NogVita.Domain.Users;
using NogVita.Infrastructure.Security;

namespace NogVita.UnitTests.Infrastructure.Security;

[Trait(TestCategories.Key, TestCategories.Slow)] // o PBKDF2 real, com 100 mil iterações, é lento de propósito
public class IdentityPasswordHasherTests
{
    private const string Password = "uma senha bem longa aqui";

    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Should_Accept_Correct_Password()
    {
        var hash = _hasher.Hash(Password);

        Assert.Equal(PasswordCheck.Success, _hasher.Verify(hash, Password));
    }

    [Fact]
    public void Should_Refuse_Wrong_Password()
    {
        var hash = _hasher.Hash(Password);

        Assert.Equal(PasswordCheck.Failed, _hasher.Verify(hash, "outra senha qualquer"));
    }

    [Fact]
    public void Should_Fail_Without_Hash()
    {
        Assert.Equal(PasswordCheck.Failed, _hasher.Verify(null, Password));
    }

    [Fact]
    public void Should_Ask_For_Rehash_When_Hash_Uses_Fewer_Iterations()
    {
        var oldHasher = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions { IterationCount = 1_000 }));
        var oldHash = oldHasher.HashPassword(null!, Password);

        Assert.Equal(PasswordCheck.SuccessRehashNeeded, _hasher.Verify(oldHash, Password));
    }
}
