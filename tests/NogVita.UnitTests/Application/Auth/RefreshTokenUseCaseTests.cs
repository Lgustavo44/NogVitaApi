using NogVita.Application.Auth;
using NogVita.Domain.Auth;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Auth;

public class RefreshTokenUseCaseTests
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

    private RefreshTokenUseCase CreateUseCase(User? user)
    {
        var tokenIssuer = new TokenIssuer(
            new FakeJwtTokenGenerator(),
            _secureTokenService,
            _refreshTokenRepository,
            new RefreshTokenSettings { ExpirationDays = 7 },
            _timeProvider);

        return new RefreshTokenUseCase(
            _refreshTokenRepository,
            new FakeUserRepository(user),
            _secureTokenService,
            tokenIssuer,
            _unitOfWork,
            _timeProvider);
    }

    [Fact]
    public async Task Should_Rotate_Tokens_When_Refresh_Token_Is_Valid()
    {
        var user = CreateActiveUser();
        var oldToken = StoreToken(user);
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(new RefreshRequest(StoredTokenValue), TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.NotEqual(StoredTokenValue, response.RefreshToken);
        Assert.True(oldToken.IsRevoked);
        Assert.Equal(2, _refreshTokenRepository.Tokens.Count);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Null_When_Token_Not_Found()
    {
        var useCase = CreateUseCase(CreateActiveUser());

        var response = await useCase.ExecuteAsync(new RefreshRequest("token-que-nao-existe"), TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Revoke_All_Sessions_When_Revoked_Token_Is_Reused()
    {
        var user = CreateActiveUser();
        var reusedToken = StoreToken(user);
        var otherSession = StoreToken(user, "outra-sessao");
        reusedToken.Revoke(DateTime.UtcNow);
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(new RefreshRequest(StoredTokenValue), TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.True(otherSession.IsRevoked);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Null_When_Token_Is_Expired()
    {
        var user = CreateActiveUser();
        StoreToken(user);
        _timeProvider.Now = DateTimeOffset.UtcNow.AddDays(8);
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(new RefreshRequest(StoredTokenValue), TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Single(_refreshTokenRepository.Tokens);
    }

    [Fact]
    public async Task Should_Revoke_Sessions_When_User_Is_Inactive()
    {
        var user = CreateActiveUser();
        var token = StoreToken(user);
        user.Deactivate();
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(new RefreshRequest(StoredTokenValue), TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.True(token.IsRevoked);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

}