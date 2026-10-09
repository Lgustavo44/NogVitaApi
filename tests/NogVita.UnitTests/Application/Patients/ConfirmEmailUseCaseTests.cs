using NogVita.Application.Patients;
using NogVita.Domain.Auth;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Patients;

public class ConfirmEmailUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeEmailConfirmationTokenRepository _tokenRepository = new();
    private readonly FakeSecureTokenService _secureTokenService = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DateTime _now = DateTime.UtcNow;

    private ConfirmEmailUseCase CreateUseCase() =>
        new(_tokenRepository, _userRepository, _secureTokenService, _unitOfWork, TimeProvider.System);

    private User CreatePendingUser()
    {
        var user = new User("João Souza", "joao@email.com", Cpf.Create("52998224725"));
        user.SetPasswordHash("hash");
        _userRepository.Add(user);
        return user;
    }

    private EmailConfirmationToken AddToken(User user, string token, DateTime expiresAtUtc, DateTime nowUtc)
    {
        var confirmationToken = new EmailConfirmationToken(user.Id, _secureTokenService.Hash(token), expiresAtUtc, nowUtc);
        _tokenRepository.Add(confirmationToken);
        return confirmationToken;
    }

    [Fact]
    public async Task Should_Confirm_Email_And_Activate_User()
    {
        var user = CreatePendingUser();
        var token = AddToken(user, "token-valido", _now.AddHours(24), _now);

        var confirmed = await CreateUseCase().ExecuteAsync(new ConfirmEmailRequest("token-valido"), TestContext.Current.CancellationToken);

        Assert.True(confirmed);
        Assert.True(user.IsEmailConfirmed);
        Assert.True(user.IsActive);
        Assert.True(token.IsUsed);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Reject_Unknown_Token()
    {
        var user = CreatePendingUser();

        var confirmed = await CreateUseCase().ExecuteAsync(new ConfirmEmailRequest("nao-existe"), TestContext.Current.CancellationToken);

        Assert.False(confirmed);
        Assert.False(user.IsActive);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Reject_Expired_Token()
    {
        var user = CreatePendingUser();
        AddToken(user, "token-expirado", _now.AddDays(-1), _now.AddDays(-2));

        var confirmed = await CreateUseCase().ExecuteAsync(new ConfirmEmailRequest("token-expirado"), TestContext.Current.CancellationToken);

        Assert.False(confirmed);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task Should_Reject_Token_Used_Twice()
    {
        var user = CreatePendingUser();
        AddToken(user, "token-valido", _now.AddHours(24), _now);
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(new ConfirmEmailRequest("token-valido"), TestContext.Current.CancellationToken);
        var secondAttempt = await useCase.ExecuteAsync(new ConfirmEmailRequest("token-valido"), TestContext.Current.CancellationToken);

        Assert.False(secondAttempt);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }
}