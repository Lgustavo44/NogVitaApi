using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Admin;
using NogVita.Application.FollowUps;
using NogVita.Domain.Auth;
using NogVita.Domain.FollowUps;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Admin;

public class DeactivateUserUseCaseTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FakeNutritionistRequestRepository _requestRepository = new();
    private readonly FakeNutritionPlanRepository _planRepository = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private static User CreateActiveUser()
    {
        var user = new User("João Souza", "joao@email.com", Cpf.Create("12345678909"));
        user.SetPasswordHash("hash");
        user.Activate();
        return user;
    }

    private User AddNutritionist()
    {
        var nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        nutritionist.SetPasswordHash("hash");
        nutritionist.ConfirmEmail(DateTime.UtcNow);
        nutritionist.CreateNutritionistProfile(1, "123456");
        nutritionist.NutritionistProfile!.Activate();
        _userRepository.Add(nutritionist);
        return nutritionist;
    }

    private User AddPatient(string email, string cpf)
    {
        var patient = new User("Paciente Teste", email, Cpf.Create(cpf));
        patient.SetPasswordHash("hash");
        patient.ConfirmEmail(DateTime.UtcNow);
        patient.CreatePatientProfile(new DateOnly(1995, 3, 10), BiologicalSex.Female, 165, Goal.WeightLoss);
        _userRepository.Add(patient);
        return patient;
    }

    private CareRelationship Follow(User patient, User nutritionist)
    {
        var relationship = new NutritionistRequest(patient.Id, nutritionist.Id, null).Accept(DateTime.UtcNow);
        _relationshipRepository.Add(relationship);
        return relationship;
    }

    private DeactivateUserUseCase CreateUseCase() =>
        new(
            _userRepository,
            _refreshTokenRepository,
            _relationshipRepository,
            _requestRepository,
            _planRepository,
            new FollowUpNotificationService(_emailSender, NullLogger<FollowUpNotificationService>.Instance),
            _unitOfWork,
            TimeProvider.System);

    [Fact]
    public async Task Should_Deactivate_User_And_Revoke_All_Sessions()
    {
        var user = CreateActiveUser();
        _userRepository.Add(user);
        var session = new RefreshToken(user.Id, "hash-da-sessao", DateTime.UtcNow.AddDays(7));
        _refreshTokenRepository.Add(session);

        var result = await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(DeactivateUserResult.Success, result);
        Assert.False(user.IsActive);
        Assert.True(session.IsRevoked);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Deactivate_Own_Account()
    {
        var admin = CreateActiveUser();
        _userRepository.Add(admin);

        var result = await CreateUseCase().ExecuteAsync(admin.Id, admin.Id, TestContext.Current.CancellationToken);

        Assert.Equal(DeactivateUserResult.CannotDeactivateSelf, result);
        Assert.True(admin.IsActive);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Not_Found_For_Unknown_User()
    {
        var result = await CreateUseCase().ExecuteAsync(AdminId, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(DeactivateUserResult.NotFound, result);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Deactivating_Nutritionist_Should_End_Every_Follow_Up_In_The_Admin_Name()
    {
        var nutritionist = AddNutritionist();
        var first = Follow(AddPatient("ana@email.com", "11144477735"), nutritionist);
        var second = Follow(AddPatient("bia@email.com", "52998224725"), nutritionist);

        await CreateUseCase().ExecuteAsync(AdminId, nutritionist.Id, TestContext.Current.CancellationToken);

        Assert.All([first, second], relationship =>
        {
            Assert.False(relationship.IsActive);
            Assert.Equal(AdminId, relationship.EndedByUserId);
        });
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Deactivating_Nutritionist_Should_Finish_Active_Plan_And_Cancel_Drafts()
    {
        var nutritionist = AddNutritionist();
        var patient = AddPatient("ana@email.com", "11144477735");
        Follow(patient, nutritionist);
        var activePlan = new NutritionPlan(patient.Id, nutritionist.Id, "Ativo", null, new DateOnly(2026, 10, 1), null);
        activePlan.Activate();
        var draft = new NutritionPlan(patient.Id, nutritionist.Id, "Rascunho", null, new DateOnly(2026, 11, 1), null);
        _planRepository.Add(activePlan);
        _planRepository.Add(draft);

        await CreateUseCase().ExecuteAsync(AdminId, nutritionist.Id, TestContext.Current.CancellationToken);

        Assert.Equal(NutritionPlanStatus.Finished, activePlan.Status);
        Assert.Equal(NutritionPlanStatus.Cancelled, draft.Status);
    }

    [Fact]
    public async Task Deactivating_Nutritionist_Should_Reject_Pending_Requests()
    {
        var nutritionist = AddNutritionist();
        var patient = AddPatient("ana@email.com", "11144477735");
        var request = new NutritionistRequest(patient.Id, nutritionist.Id, "Oi!");
        _requestRepository.Add(request);

        await CreateUseCase().ExecuteAsync(AdminId, nutritionist.Id, TestContext.Current.CancellationToken);

        Assert.Equal(NutritionistRequestStatus.Rejected, request.Status);
        Assert.False(await _requestRepository.HasPendingForPatientAsync(patient.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Deactivating_Nutritionist_Should_Email_Each_Patient()
    {
        var nutritionist = AddNutritionist();
        Follow(AddPatient("ana@email.com", "11144477735"), nutritionist);
        Follow(AddPatient("bia@email.com", "52998224725"), nutritionist);

        await CreateUseCase().ExecuteAsync(AdminId, nutritionist.Id, TestContext.Current.CancellationToken);

        Assert.Equal(["ana@email.com", "bia@email.com"], _emailSender.SentMessages.Select(m => m.To).Order());
        Assert.All(_emailSender.SentMessages, m => Assert.Contains("Nutri Teste", m.TextBody));
    }

    [Fact]
    public async Task Deactivating_Patient_Should_Not_End_Follow_Up()
    {
        var nutritionist = AddNutritionist();
        var patient = AddPatient("ana@email.com", "11144477735");
        var relationship = Follow(patient, nutritionist);

        await CreateUseCase().ExecuteAsync(AdminId, patient.Id, TestContext.Current.CancellationToken);

        Assert.True(relationship.IsActive);
        Assert.Empty(_emailSender.SentMessages);
    }
}
