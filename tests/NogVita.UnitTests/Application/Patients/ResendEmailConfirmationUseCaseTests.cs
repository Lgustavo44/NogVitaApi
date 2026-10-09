using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.Patients;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Patients;

public class ResendEmailConfirmationUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeEmailConfirmationTokenRepository _tokenRepository = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ResendEmailConfirmationUseCase CreateUseCase()
    {
        var emailConfirmationService = new EmailConfirmationService(
            _tokenRepository,
            new FakeSecureTokenService(),
            _emailSender,
            new FrontendSettings { BaseUrl = "http://localhost:5173" },
            TimeProvider.System,
            NullLogger<EmailConfirmationService>.Instance);

        return new ResendEmailConfirmationUseCase(_userRepository, emailConfirmationService, _unitOfWork);
    }

    private User AddUser(bool confirmed)
    {
        var user = new User("João Souza", "joao@email.com", Cpf.Create("52998224725"));
        user.SetPasswordHash("hash");
        if (confirmed)
        {
            user.ConfirmEmail(DateTime.UtcNow);
        }
        _userRepository.Add(user);
        return user;
    }

    [Fact]
    public async Task Should_Send_New_Link_For_Pending_User()
    {
        AddUser(confirmed: false);

        await CreateUseCase().ExecuteAsync(new ResendEmailConfirmationRequest("joao@email.com"), TestContext.Current.CancellationToken);

        Assert.Single(_tokenRepository.Tokens);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
        Assert.Single(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Should_Stay_Silent_For_Unknown_Email()
    {
        await CreateUseCase().ExecuteAsync(new ResendEmailConfirmationRequest("ninguem@email.com"), TestContext.Current.CancellationToken);

        Assert.Empty(_emailSender.SentMessages);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Stay_Silent_For_Confirmed_User()
    {
        AddUser(confirmed: true);

        await CreateUseCase().ExecuteAsync(new ResendEmailConfirmationRequest("joao@email.com"), TestContext.Current.CancellationToken);

        Assert.Empty(_emailSender.SentMessages);
        Assert.Empty(_tokenRepository.Tokens);
    }
}