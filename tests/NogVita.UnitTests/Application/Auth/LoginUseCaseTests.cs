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
        var user = TestData.CreateUser();
        user.SetPasswordHash(PasswordHasher.Hash(ValidPassword));
        return user;
    }

    // Confirmar o e-mail também ativa a conta.
    private static User CreateActiveUser()
    {
        var user = CreateUserWithPassword();
        user.ConfirmEmail(DateTime.UtcNow);
        return user;
    }

    // Instância por teste, para contar as verificações sem interferência dos outros testes.
    private readonly FakePasswordHasher _passwordHasher = new();
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

        return new LoginUseCase(new FakeUserRepository(user), _passwordHasher, tokenIssuer, _unitOfWork);
    }

    [Fact]
    public async Task Should_Verify_Password_Even_When_User_Not_Found()
    {
        var useCase = CreateUseCase(null);

        await useCase.ExecuteAsync(new LoginRequest(ValidEmail, ValidPassword), TestContext.Current.CancellationToken);

        // O mesmo custo de quem existe: o tempo de resposta não revela o e-mail.
        Assert.Equal(1, _passwordHasher.VerifyCount);
    }

    [Fact]
    public async Task Should_Verify_Password_Even_When_User_Has_No_Password()
    {
        var user = TestData.CreateUser(); // pré-cadastro de nutricionista: conta sem senha
        var useCase = CreateUseCase(user);

        var result = await useCase.ExecuteAsync(new LoginRequest(ValidEmail, ValidPassword), TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Equal(1, _passwordHasher.VerifyCount);
    }

    [Fact]
    public async Task Should_Rehash_Outdated_Password_On_Successful_Login()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash($"{FakePasswordHasher.OutdatedPrefix}{ValidPassword}");
        user.ConfirmEmail(DateTime.UtcNow);
        var useCase = CreateUseCase(user);

        var result = await useCase.ExecuteAsync(new LoginRequest(ValidEmail, ValidPassword), TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.Succeeded, result.Status);
        Assert.Equal(_passwordHasher.Hash(ValidPassword), user.PasswordHash);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Rehash_When_Login_Is_Refused()
    {
        const string outdatedHash = $"{FakePasswordHasher.OutdatedPrefix}{ValidPassword}";
        var user = TestData.CreateUser();
        user.SetPasswordHash(outdatedHash);
        user.ConfirmEmail(DateTime.UtcNow);
        user.Deactivate();
        var useCase = CreateUseCase(user);

        var result = await useCase.ExecuteAsync(new LoginRequest(ValidEmail, ValidPassword), TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Equal(outdatedHash, user.PasswordHash);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Keep_Current_Hash_When_No_Rehash_Is_Needed()
    {
        var user = CreateActiveUser();
        var originalHash = user.PasswordHash;
        var useCase = CreateUseCase(user);

        await useCase.ExecuteAsync(new LoginRequest(ValidEmail, ValidPassword), TestContext.Current.CancellationToken);

        Assert.Equal(originalHash, user.PasswordHash);
    }

    [Fact]
    public async Task Should_Return_Token_For_Valid_Credentials()
    {
        var user = CreateActiveUser();
        var useCase = CreateUseCase(user);

        var result = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.Succeeded, result.Status);
        Assert.NotNull(result.Tokens);
        Assert.Equal($"token-for-{user.Id}", result.Tokens.AccessToken);
    }

    [Fact]
    public async Task Should_Return_Invalid_Credentials_When_User_Not_Found()
    {
        var useCase = CreateUseCase(null);

        var result = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Empty(_refreshTokenRepository.Tokens);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Invalid_Credentials_When_Password_Is_Wrong()
    {
        var useCase = CreateUseCase(CreateActiveUser());

        var result = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, "senha-errada"),
            TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Empty(_refreshTokenRepository.Tokens);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Invalid_Credentials_When_User_Is_Inactive()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        var useCase = CreateUseCase(user);

        var result = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Empty(_refreshTokenRepository.Tokens);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Email_Not_Confirmed_When_Password_Is_Correct()
    {
        var useCase = CreateUseCase(CreateUserWithPassword());

        var result = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.EmailNotConfirmed, result.Status);
        Assert.Null(result.Tokens);
        Assert.Empty(_refreshTokenRepository.Tokens);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Reveal_Unconfirmed_Email_When_Password_Is_Wrong()
    {
        var useCase = CreateUseCase(CreateUserWithPassword());

        var result = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, "senha-errada"),
            TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
    }

    [Fact]
    public async Task Should_Return_Tokens_And_Save_Refresh_Token_For_Valid_Credentials()
    {
        var user = CreateActiveUser();
        var useCase = CreateUseCase(user);

        var result = await useCase.ExecuteAsync(
            new LoginRequest(ValidEmail, ValidPassword),
            TestContext.Current.CancellationToken);

        Assert.Equal(LoginStatus.Succeeded, result.Status);
        Assert.NotNull(result.Tokens);
        Assert.Equal($"token-for-{user.Id}", result.Tokens.AccessToken);

        var savedToken = Assert.Single(_refreshTokenRepository.Tokens);
        Assert.Equal(_secureTokenService.Hash(result.Tokens.RefreshToken), savedToken.TokenHash);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }
}
