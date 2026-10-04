using NogVita.Application.Auth;
using NogVita.Domain.Auth;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Auth;

public class LogoutUseCaseTests
{
    private const string StoredTokenValue = "refresh-token-atual";

    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new();
    private readonly FakeSecureTokenService _secureTokenService = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FixedTimeProvider _timeProvider = new(DateTimeOffset.UtcNow);

    private static User CreateActiveUser()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        user.SetPasswordHash("hash");
        user.Activate();
        return user;
    }

    private RefreshToken StoreToken(User user, string value = StoredTokenValue)
    {
        var token = new RefreshToken(user.Id, _secureTokenService.Hash(value), DateTime.UtcNow.AddDays(7));
        _refreshTokenRepository.Add(token);
        return token;
    }

    private LogoutUseCase CreateUseCase() =>
        new(_refreshTokenRepository, _secureTokenService, _unitOfWork, _timeProvider);

    [Fact]
    public async Task Should_Revoke_Token_On_Logout()
    {
        var user = CreateActiveUser();
        var token = StoreToken(user);
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(new LogoutRequest(StoredTokenValue), TestContext.Current.CancellationToken);

        Assert.True(token.IsRevoked);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Do_Nothing_When_Token_Not_Found()
    {
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(new LogoutRequest("token-que-nao-existe"), TestContext.Current.CancellationToken);

        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Revoke_Other_Sessions_When_Token_Already_Revoked()
    {
        var user = CreateActiveUser();
        var revokedToken = StoreToken(user);
        var otherSession = StoreToken(user, "outra-sessao");
        revokedToken.Revoke(DateTime.UtcNow);
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(new LogoutRequest(StoredTokenValue), TestContext.Current.CancellationToken);

        Assert.False(otherSession.IsRevoked);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}