using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.Nutritionists;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Nutritionists;

public class PreRegisterNutritionistUseCaseTests
{
    private const string AnaCpf = "123.456.789-09";
    private const string AnaEmail = "ana@nogvita.local";

    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeInvitationRepository _invitationRepository = new();
    private readonly FakeSecureTokenService _secureTokenService = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private static PreRegisterNutritionistRequest CreateRequest(string cpf = AnaCpf, string email = AnaEmail, string crnNumber = "12345") =>
        new("Ana Lima", email, cpf, 3, crnNumber);

    private PreRegisterNutritionistUseCase CreateUseCase()
    {
        var invitationService = new NutritionistInvitationService(
            _invitationRepository,
            _secureTokenService,
            _emailSender,
            new FrontendSettings { BaseUrl = "http://localhost:4200" },
            TimeProvider.System,
            NullLogger<NutritionistInvitationService>.Instance);

        return new PreRegisterNutritionistUseCase(_userRepository, _unitOfWork, invitationService);
    }

    private static User CreateActivePatient(string cpf, string email)
    {
        var user = new User("Ana Lima", email, Cpf.Create(cpf));
        user.SetPasswordHash("hash-da-senha-atual");
        user.Activate();
        user.CreatePatientProfile(new DateOnly(1990, 1, 1), BiologicalSex.Female, 165, Goal.Maintenance);
        return user;
    }

    [Fact]
    public async Task Should_Create_Inactive_User_Without_Password_When_New()
    {
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(PreRegisterNutritionistStatus.Created, result.Status);
        var user = Assert.Single(_userRepository.Users);
        Assert.False(user.IsActive);
        Assert.Null(user.PasswordHash);
        Assert.NotNull(user.NutritionistProfile);
        Assert.False(user.NutritionistProfile.IsActive);
        Assert.Single(_invitationRepository.Invitations);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Add_Profile_To_Existing_Patient_And_Keep_Password()
    {
        var maria = CreateActivePatient(AnaCpf, AnaEmail);
        _userRepository.Add(maria);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(PreRegisterNutritionistStatus.Created, result.Status);
        Assert.Single(_userRepository.Users);
        Assert.Equal(maria.Id, result.UserId);
        Assert.Equal("hash-da-senha-atual", maria.PasswordHash);
        Assert.True(maria.IsActive);
        Assert.NotNull(maria.NutritionistProfile);
        Assert.False(maria.NutritionistProfile.IsActive);
    }

    [Fact]
    public async Task Should_Conflict_When_Cpf_Matches_But_Email_Differs()
    {
        _userRepository.Add(CreateActivePatient(AnaCpf, "outro@nogvita.local"));
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(PreRegisterNutritionistStatus.Conflict, result.Status);
        Assert.Empty(_invitationRepository.Invitations);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Conflict_When_Email_Matches_But_Cpf_Differs()
    {
        _userRepository.Add(CreateActivePatient("529.982.247-25", AnaEmail));
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(PreRegisterNutritionistStatus.Conflict, result.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Conflict_When_User_Is_Already_Nutritionist()
    {
        var ana = CreateActivePatient(AnaCpf, AnaEmail);
        ana.CreateNutritionistProfile(5, "99999");
        _userRepository.Add(ana);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(PreRegisterNutritionistStatus.Conflict, result.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Conflict_When_Crn_Already_Exists()
    {
        var other = CreateActivePatient("529.982.247-25", "outra@nogvita.local");
        other.CreateNutritionistProfile(3, "12345");
        _userRepository.Add(other);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(CreateRequest(crnNumber: " 12345 "), TestContext.Current.CancellationToken);

        Assert.Equal(PreRegisterNutritionistStatus.Conflict, result.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Send_Email_With_Link_Matching_Stored_Invitation()
    {
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Equal(AnaEmail, email.To);

        var token = email.TextBody.Split("#token=")[1].Split(Environment.NewLine)[0].Trim();
        var invitation = Assert.Single(_invitationRepository.Invitations);
        Assert.Equal(_secureTokenService.Hash(token), invitation.TokenHash);
    }

    [Fact]
    public async Task Should_Keep_Pre_Registration_When_Email_Fails()
    {
        _emailSender.ShouldFail = true;
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(PreRegisterNutritionistStatus.Created, result.Status);
        Assert.False(result.InvitationEmailSent);
        Assert.Single(_userRepository.Users);
        Assert.Single(_invitationRepository.Invitations);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }
}