using NogVita.Application.Auth;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Auth;

public class LoginUseCaseTests
{
    private const string ValidEmail = "maria@email.com";
    private const string ValidPassword = "senha-correta-123";

    private static readonly FakePasswordHasher PasswordHasher = new();

    private static User CreateUserWithPassword()
    {
        var user = new User("Maria Silva", ValidEmail, "12345678909");
        user.SetPasswordHash(PasswordHasher.Hash(ValidPassword));
        return user;
    }

    private static User CreateActiveUser()
    {
        var user = CreateUserWithPassword();
        user.Activate();
        return user;
    }

    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeSecureTokenService _secureTokenService = new();

    private LoginUseCase CreateUseCase(User? user)
    {
        var tokenIssuer = new TokenIssuer(
            new FakeJwtTokenGenerator(),
            _secureTokenService,
            _refreshTokenRepository,
            new RefreshTokenSettings { ExpirationDays = 7 },
            TimeProvider.System);

        return new LoginUseCase(new FakeUserRepository(user), PasswordHasher, tokenIssuer, _unitOfWork);
    }

    [Fact]
    public async Task Should_Return_Token_For_Valid_Credentials()
    {
        var user = CreateActiveUser();
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal($"token-for-{user.Id}", response.AccessToken);
    }

    [Fact]
    public async Task Should_Return_Null_When_User_Not_Found()
    {
        var useCase = CreateUseCase(null);

        var response = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Empty(_refreshTokenRepository.Tokens);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Null_When_Password_Is_Wrong()
    {
        var useCase = CreateUseCase(CreateActiveUser());

        var response = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, "senha-errada"),
            TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Empty(_refreshTokenRepository.Tokens);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Null_When_User_Is_Inactive()
    {
        var useCase = CreateUseCase(CreateUserWithPassword());

        var response = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Empty(_refreshTokenRepository.Tokens);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Tokens_And_Save_Refresh_Token_For_Valid_Credentials()
    {
        var user = CreateActiveUser();
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal($"token-for-{user.Id}", response.AccessToken);

        var savedToken = Assert.Single(_refreshTokenRepository.Tokens);
        Assert.Equal(_secureTokenService.Hash(response.RefreshToken), savedToken.TokenHash);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }
}