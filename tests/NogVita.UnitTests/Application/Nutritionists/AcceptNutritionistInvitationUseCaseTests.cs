using NogVita.Application.Nutritionists;
using NogVita.Domain.Invitations;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Nutritionists;

public class AcceptNutritionistInvitationUseCaseTests
{
    private const string TokenValue = "token-do-convite";
    private const string NewPassword = "cafe-com-pao-de-queijo";

    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeInvitationRepository _invitationRepository = new();
    private readonly FakeSecureTokenService _secureTokenService = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FixedTimeProvider _timeProvider = new(Now);

    private AcceptNutritionistInvitationUseCase CreateUseCase() =>
        new(_invitationRepository, _userRepository, _secureTokenService, _passwordHasher, _unitOfWork, _timeProvider);

    private User AddNewNutritionist()
    {
        var user = new User("Ana Lima", "ana@nogvita.local", Cpf.Create("123.456.789-09"));
        user.CreateNutritionistProfile(3, "12345");
        _userRepository.Add(user);
        return user;
    }

    private User AddExistingPatientWithNutritionistProfile()
    {
        var user = new User("Maria Silva", "maria@nogvita.local", Cpf.Create("529.982.247-25"));
        user.SetPasswordHash("hash-da-senha-atual");
        user.Activate();
        user.CreatePatientProfile(new DateOnly(1990, 1, 1), BiologicalSex.Female, 165, Goal.Maintenance);
        user.CreateNutritionistProfile(3, "54321");
        _userRepository.Add(user);
        return user;
    }

    private NutritionistInvitation AddInvitation(User user)
    {
        var now = Now.UtcDateTime;
        var invitation = new NutritionistInvitation(user.Id, _secureTokenService.Hash(TokenValue), now.AddHours(72), now);
        _invitationRepository.Add(invitation);
        return invitation;
    }

    [Fact]
    public async Task Should_Define_Password_And_Activate_New_User()
    {
        var user = AddNewNutritionist();
        var invitation = AddInvitation(user);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(new AcceptInvitationRequest(TokenValue, NewPassword), TestContext.Current.CancellationToken);

        Assert.Equal(AcceptInvitationResult.Accepted, result);
        Assert.Equal(_passwordHasher.Hash(NewPassword), user.PasswordHash);
        Assert.True(user.IsActive);
        Assert.True(user.IsEmailConfirmed);
        Assert.True(user.NutritionistProfile!.IsActive);
        Assert.True(invitation.IsUsed);
        Assert.Contains(Roles.Nutritionist, user.GetRoles());
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Require_Password_For_New_User_And_Keep_Invitation_Valid()
    {
        var user = AddNewNutritionist();
        var invitation = AddInvitation(user);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(new AcceptInvitationRequest(TokenValue, null), TestContext.Current.CancellationToken);

        Assert.Equal(AcceptInvitationResult.PasswordRequired, result);
        Assert.False(invitation.IsUsed);
        Assert.False(user.IsActive);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Change_Password_Of_Existing_User()
    {
        var maria = AddExistingPatientWithNutritionistProfile();
        AddInvitation(maria);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(new AcceptInvitationRequest(TokenValue, "outra-senha-qualquer-longa"), TestContext.Current.CancellationToken);

        Assert.Equal(AcceptInvitationResult.Accepted, result);
        Assert.Equal("hash-da-senha-atual", maria.PasswordHash);
        Assert.True(maria.NutritionistProfile!.IsActive);
        Assert.Contains(Roles.Patient, maria.GetRoles());
        Assert.Contains(Roles.Nutritionist, maria.GetRoles());
    }

    [Fact]
    public async Task Should_Reject_Expired_Invitation()
    {
        var user = AddNewNutritionist();
        AddInvitation(user);
        _timeProvider.Now = Now.AddHours(73);
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(new AcceptInvitationRequest(TokenValue, NewPassword), TestContext.Current.CancellationToken);

        Assert.Equal(AcceptInvitationResult.InvalidInvitation, result);
        Assert.False(user.IsActive);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Reject_Used_Invitation()
    {
        var user = AddNewNutritionist();
        var invitation = AddInvitation(user);
        invitation.MarkAsUsed(Now.UtcDateTime.AddHours(1));
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(new AcceptInvitationRequest(TokenValue, NewPassword), TestContext.Current.CancellationToken);

        Assert.Equal(AcceptInvitationResult.InvalidInvitation, result);
    }

    [Fact]
    public async Task Should_Reject_Revoked_Invitation()
    {
        var user = AddNewNutritionist();
        var invitation = AddInvitation(user);
        invitation.Revoke(Now.UtcDateTime.AddHours(1));
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(new AcceptInvitationRequest(TokenValue, NewPassword), TestContext.Current.CancellationToken);

        Assert.Equal(AcceptInvitationResult.InvalidInvitation, result);
    }

    [Fact]
    public async Task Should_Reject_Unknown_Token()
    {
        AddNewNutritionist();
        var useCase = CreateUseCase();

        var result = await useCase.ExecuteAsync(new AcceptInvitationRequest("token-que-nao-existe", NewPassword), TestContext.Current.CancellationToken);

        Assert.Equal(AcceptInvitationResult.InvalidInvitation, result);
    }
}