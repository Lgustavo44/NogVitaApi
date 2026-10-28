using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Auth;
using NogVita.Application.Common;
using NogVita.Domain.Auth;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Auth;

public class PasswordResetUseCaseTests
{
    private const string NewPassword = "uma-senha-nova-bem-longa";

    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordResetTokenRepository _tokenRepository = new();
    private readonly FakeSecureTokenService _secureTokenService = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private PasswordResetNotificationService CreateNotificationService() =>
        new(_emailSender, new FrontendSettings { BaseUrl = "https://nogvita-green.vercel.app" },
            NullLogger<PasswordResetNotificationService>.Instance);

    private RequestPasswordResetUseCase CreateRequestUseCase() =>
        new(_userRepository, _tokenRepository, _secureTokenService, CreateNotificationService(), _unitOfWork, TimeProvider.System);

    private ConfirmPasswordResetUseCase CreateConfirmUseCase() =>
        new(_tokenRepository, _userRepository, _secureTokenService, _passwordHasher, _refreshTokenRepository,
            CreateNotificationService(), _unitOfWork, TimeProvider.System);

    private User AddUser(bool confirmed = true, bool withPassword = true)
    {
        var user = new User("João Souza", "joao@email.com", Cpf.Create("12345678909"));
        if (withPassword)
        {
            user.SetPasswordHash("hash-antigo");
            if (confirmed)
            {
                user.ConfirmEmail(DateTime.UtcNow);
            }
        }
        _userRepository.Add(user);
        return user;
    }

    private void AddToken(User user, string token, DateTime expiresAtUtc, DateTime nowUtc) =>
        _tokenRepository.Add(new PasswordResetToken(user.Id, _secureTokenService.Hash(token), expiresAtUtc, nowUtc));

    [Fact]
    public async Task Request_Should_Send_Link_To_Active_User()
    {
        AddUser();

        await CreateRequestUseCase().ExecuteAsync(new RequestPasswordResetRequest("joao@email.com"), TestContext.Current.CancellationToken);

        Assert.Single(_tokenRepository.Tokens);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Contains("/redefinir-senha#token=", email.TextBody);
    }

    [Fact]
    public async Task Request_Should_Revoke_Previous_Link()
    {
        AddUser();
        var useCase = CreateRequestUseCase();

        await useCase.ExecuteAsync(new RequestPasswordResetRequest("joao@email.com"), TestContext.Current.CancellationToken);
        await useCase.ExecuteAsync(new RequestPasswordResetRequest("joao@email.com"), TestContext.Current.CancellationToken);

        Assert.Equal(2, _tokenRepository.Tokens.Count);
        Assert.Single(_tokenRepository.Tokens, t => t.IsValid(DateTime.UtcNow));
    }

    [Fact]
    public async Task Request_Should_Stay_Silent_For_Unknown_Email()
    {
        await CreateRequestUseCase().ExecuteAsync(new RequestPasswordResetRequest("ninguem@email.com"), TestContext.Current.CancellationToken);

        Assert.Empty(_emailSender.SentMessages);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Request_Should_Stay_Silent_For_User_Without_Password()
    {
        AddUser(withPassword: false);

        await CreateRequestUseCase().ExecuteAsync(new RequestPasswordResetRequest("joao@email.com"), TestContext.Current.CancellationToken);

        Assert.Empty(_emailSender.SentMessages);
        Assert.Empty(_tokenRepository.Tokens);
    }

    [Fact]
    public async Task Request_Should_Stay_Silent_For_Deactivated_User()
    {
        var user = AddUser();
        user.Deactivate(DateTime.UtcNow);

        await CreateRequestUseCase().ExecuteAsync(new RequestPasswordResetRequest("joao@email.com"), TestContext.Current.CancellationToken);

        Assert.Empty(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Confirm_Should_Change_Password_And_End_All_Sessions()
    {
        var user = AddUser();
        var now = DateTime.UtcNow;
        AddToken(user, "token-valido", now.AddHours(1), now);

        var changed = await CreateConfirmUseCase().ExecuteAsync(
            new ConfirmPasswordResetRequest("token-valido", NewPassword), TestContext.Current.CancellationToken);

        Assert.True(changed);
        Assert.Equal(_passwordHasher.Hash(NewPassword), user.PasswordHash);
        Assert.Contains(user.Id, _refreshTokenRepository.RevokedUserIds);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Equal("Sua senha do NogVita foi alterada", email.Subject);
    }

    [Fact]
    public async Task Confirm_Should_Confirm_Email_Of_Unconfirmed_User()
    {
        var user = AddUser(confirmed: false);
        var now = DateTime.UtcNow;
        AddToken(user, "token-valido", now.AddHours(1), now);

        await CreateConfirmUseCase().ExecuteAsync(
            new ConfirmPasswordResetRequest("token-valido", NewPassword), TestContext.Current.CancellationToken);

        Assert.True(user.IsEmailConfirmed);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Confirm_Should_Reject_Expired_Token()
    {
        var user = AddUser();
        var now = DateTime.UtcNow;
        AddToken(user, "token-expirado", now.AddHours(-1), now.AddHours(-2));

        var changed = await CreateConfirmUseCase().ExecuteAsync(
            new ConfirmPasswordResetRequest("token-expirado", NewPassword), TestContext.Current.CancellationToken);

        Assert.False(changed);
        Assert.Equal("hash-antigo", user.PasswordHash);
    }

    [Fact]
    public async Task Confirm_Should_Not_Work_Twice()
    {
        var user = AddUser();
        var now = DateTime.UtcNow;
        AddToken(user, "token-valido", now.AddHours(1), now);
        var useCase = CreateConfirmUseCase();
        await useCase.ExecuteAsync(new ConfirmPasswordResetRequest("token-valido", NewPassword), TestContext.Current.CancellationToken);

        var secondAttempt = await useCase.ExecuteAsync(
            new ConfirmPasswordResetRequest("token-valido", "outra-senha-bem-longa-aqui"), TestContext.Current.CancellationToken);

        Assert.False(secondAttempt);
        Assert.Equal(_passwordHasher.Hash(NewPassword), user.PasswordHash);
    }

    [Fact]
    public async Task Confirm_Should_Reject_When_Admin_Deactivated_After_Request()
    {
        var user = AddUser();
        var now = DateTime.UtcNow;
        AddToken(user, "token-valido", now.AddHours(1), now);
        user.Deactivate(DateTime.UtcNow);

        var changed = await CreateConfirmUseCase().ExecuteAsync(
            new ConfirmPasswordResetRequest("token-valido", NewPassword), TestContext.Current.CancellationToken);

        Assert.False(changed);
        Assert.Equal("hash-antigo", user.PasswordHash);
    }

    // O furo da Fase 11B (D163): sem o bloqueio explícito, uma conta desativada antes de confirmar o e-mail
    // parecia "só não confirmada", recebia o link, e a redefinição confirmava o e-mail e reativava a conta.
    [Fact]
    public async Task Request_Should_Stay_Silent_For_Blocked_User_With_Unconfirmed_Email()
    {
        var user = AddUser(confirmed: false);
        user.Deactivate(DateTime.UtcNow);

        await CreateRequestUseCase().ExecuteAsync(new RequestPasswordResetRequest("joao@email.com"), TestContext.Current.CancellationToken);

        Assert.Empty(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Confirm_Should_Not_Reactivate_Blocked_User_With_Unconfirmed_Email()
    {
        var user = AddUser(confirmed: false);
        var now = DateTime.UtcNow;
        AddToken(user, "token-valido", now.AddHours(1), now);
        user.Deactivate(now);

        var changed = await CreateConfirmUseCase().ExecuteAsync(
            new ConfirmPasswordResetRequest("token-valido", NewPassword), TestContext.Current.CancellationToken);

        Assert.False(changed);
        Assert.False(user.IsActive);
        Assert.Equal("hash-antigo", user.PasswordHash);
    }
}
