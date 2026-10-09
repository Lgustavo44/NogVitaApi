using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.Patients;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Patients;

public class RegisterPatientUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeEmailConfirmationTokenRepository _tokenRepository = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private static RegisterPatientRequest CreateRequest(string email = "joao@email.com", string cpf = "529.982.247-25") =>
        new("João Souza", email, cpf, "cafe-com-pao-de-queijo", new DateOnly(1995, 3, 10), BiologicalSex.Male, 178, Goal.MuscleGain);

    private RegisterPatientUseCase CreateUseCase()
    {
        var emailConfirmationService = new EmailConfirmationService(
            _tokenRepository,
            new FakeSecureTokenService(),
            _emailSender,
            new FrontendSettings { BaseUrl = "http://localhost:5173" },
            TimeProvider.System,
            NullLogger<EmailConfirmationService>.Instance);

        return new RegisterPatientUseCase(
            _userRepository,
            _passwordHasher,
            emailConfirmationService,
            _unitOfWork,
            NullLogger<RegisterPatientUseCase>.Instance);
    }
    [Fact]
    public async Task Should_Stay_Silent_When_Email_Belongs_To_Pending_Nutritionist()
    {
        var nutritionist = new User("Nutri Pendente", "joao@email.com", Cpf.Create("12345678909"));
        nutritionist.CreateNutritionistProfile(1, "123456");
        _userRepository.Add(nutritionist);
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Empty(_emailSender.SentMessages);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Create_Inactive_Patient_And_Send_Confirmation()
    {
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var user = Assert.Single(_userRepository.Users);
        Assert.False(user.IsActive);
        Assert.False(user.IsEmailConfirmed);
        Assert.NotNull(user.PatientProfile);
        Assert.Equal(_passwordHasher.Hash("cafe-com-pao-de-queijo"), user.PasswordHash);
        Assert.Single(_tokenRepository.Tokens);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);

        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Contains("/confirmar-email#token=", email.TextBody);
    }

    [Fact]
    public async Task Should_Notify_Owner_When_Email_Already_Exists()
    {
        var owner = new User("Dono da Conta", "joao@email.com", Cpf.Create("12345678909"));
        owner.SetPasswordHash("hash-do-dono");
        owner.ConfirmEmail(DateTime.UtcNow);
        _userRepository.Add(owner);
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(CreateRequest(email: "JOAO@email.com"), TestContext.Current.CancellationToken);

        Assert.Single(_userRepository.Users);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);

        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Equal("joao@email.com", email.To);
        Assert.Contains("Dono da Conta", email.TextBody);
        Assert.DoesNotContain("João Souza", email.TextBody);
    }

    [Fact]
    public async Task Should_Resend_Confirmation_When_Email_Exists_But_Is_Not_Confirmed()
    {
        var pending = new User("João Souza", "joao@email.com", Cpf.Create("529.982.247-25"));
        pending.SetPasswordHash("senha-original");
        _userRepository.Add(pending);
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal("senha-original", pending.PasswordHash);
        Assert.Single(_userRepository.Users);
        Assert.Single(_tokenRepository.Tokens);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);

        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Contains("/confirmar-email#token=", email.TextBody);
    }

    [Fact]
    public async Task Should_Notify_Cpf_Owner_When_Only_Cpf_Already_Exists()
    {
        var owner = new User("Outra Pessoa", "outra@email.com", Cpf.Create("529.982.247-25"));
        owner.SetPasswordHash("hash-do-dono");
        owner.ConfirmEmail(DateTime.UtcNow);
        _userRepository.Add(owner);
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Single(_userRepository.Users);
        Assert.Empty(_tokenRepository.Tokens);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);

        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Equal("outra@email.com", email.To);
        Assert.Contains("Outra Pessoa", email.TextBody);
        Assert.Contains("CPF", email.TextBody);
        Assert.DoesNotContain("joao@email.com", email.TextBody);
        Assert.DoesNotContain("João Souza", email.TextBody);
    }

    [Fact]
    public async Task Should_Not_Notify_Cpf_Owner_Without_Password()
    {
        _userRepository.Add(new User("Nutri Pendente", "nutri@email.com", Cpf.Create("529.982.247-25")));
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Single(_userRepository.Users);
        Assert.Empty(_emailSender.SentMessages);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Keep_Registration_When_Email_Fails()
    {
        _emailSender.ShouldFail = true;
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Single(_userRepository.Users);
        Assert.Single(_tokenRepository.Tokens);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }
}