using NogVita.Application.Admin;
using NogVita.Application.Nutritionists;
using NogVita.Domain.Audit;
using NogVita.Domain.Invitations;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Nutritionists;

public class CancelNutritionistPreRegistrationUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeInvitationRepository _invitationRepository = new();

    private readonly FakeUnitOfWork _unitOfWork = new();
    private static readonly Guid AdminId = Guid.NewGuid();
    private readonly CapturingLogger<AdminAuditLog> _auditLogger = new();
    private AdminAuditLog AuditLog => new(_auditLogger);
    private readonly FakeAdminAuditRepository _auditRepository = new();

    private CancelNutritionistPreRegistrationUseCase CreateUseCase() =>
        new(_userRepository, _invitationRepository, _auditRepository, _unitOfWork, TimeProvider.System, AuditLog);

    private User AddPendingNutritionist()
    {
        var user = TestUsers.PendingNutritionist("Carla Dias", "carla@nogvita.local");
        _userRepository.Add(user);
        return user;
    }

    private NutritionistInvitation AddInvitation(User user)
    {
        var now = DateTime.UtcNow;
        var invitation = new NutritionistInvitation(user.Id, "hash-do-convite", now.AddHours(72), now);
        _invitationRepository.Add(invitation);
        return invitation;
    }

    [Fact]
    public async Task Should_Remove_Account_Created_Only_For_The_Pre_Registration()
    {
        var user = AddPendingNutritionist();

        var status = await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(CancelPreRegistrationStatus.Cancelled, status);
        Assert.Empty(_userRepository.Users);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Free_Cpf_Email_And_Crn()
    {
        var user = AddPendingNutritionist();
        var ct = TestContext.Current.CancellationToken;

        await CreateUseCase().ExecuteAsync(AdminId, user.Id, ct);

        Assert.False(await _userRepository.ExistsByCpfAsync(Cpf.Create("123.456.789-09"), ct));
        Assert.False(await _userRepository.ExistsByEmailAsync("carla@nogvita.local", ct));
        Assert.False(await _userRepository.ExistsByCrnAsync(3, "12345", ct));
    }

    [Fact]
    public async Task Should_Keep_Patient_Account_And_Remove_Only_Nutritionist_Profile()
    {
        var user = AddPendingNutritionist();
        user.SetPasswordHash("hash");
        user.ConfirmEmail(DateTime.UtcNow);
        user.CreatePatientProfile(new DateOnly(1990, 5, 20), BiologicalSex.Female, 165, Goal.Maintenance);
        var profile = user.NutritionistProfile!;
        var invitation = AddInvitation(user);

        var status = await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(CancelPreRegistrationStatus.Cancelled, status);
        Assert.Contains(user, _userRepository.Users);
        Assert.Null(user.NutritionistProfile);
        Assert.NotNull(user.PatientProfile);
        Assert.Same(profile, Assert.Single(_userRepository.RemovedNutritionistProfiles));
        Assert.True(invitation.IsRevoked);
        Assert.False(await _userRepository.ExistsByCrnAsync(3, "12345", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Not_Cancel_Accepted_Invitation()
    {
        var user = AddPendingNutritionist();
        user.NutritionistProfile!.Activate();

        var status = await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(CancelPreRegistrationStatus.AlreadyAccepted, status);
        Assert.NotNull(user.NutritionistProfile);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Not_Found_For_User_Without_Nutritionist_Profile()
    {
        var user = new User("João Souza", "joao@nogvita.local", Cpf.Create("529.982.247-25"));
        _userRepository.Add(user);

        var status = await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(CancelPreRegistrationStatus.NotFound, status);
        Assert.Single(_userRepository.Users);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Not_Found_For_Unknown_User()
    {
        var status = await CreateUseCase().ExecuteAsync(AdminId, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(CancelPreRegistrationStatus.NotFound, status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Record_Audit_Entry_Without_Personal_Data()
    {
        var user = AddPendingNutritionist();

        await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        var entry = Assert.Single(_auditRepository.Entries);
        Assert.Equal(AdminId, entry.AdminUserId);
        Assert.Equal(AdminAuditAction.NutritionistPreRegistrationCancelled, entry.Action);
        Assert.Equal(user.Id, entry.TargetUserId);
        Assert.Contains("CRN-3 12345", entry.Details);
        Assert.Contains("Conta apagada", entry.Details);
        Assert.DoesNotContain("carla", entry.Details, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("456", entry.Details);
    }

    [Fact]
    public async Task Should_Not_Record_Audit_Entry_When_Nothing_Was_Cancelled()
    {
        var user = AddPendingNutritionist();
        user.NutritionistProfile!.Activate();

        await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Empty(_auditRepository.Entries);
        AuditAssert.Nothing(_auditLogger);
    }

    [Fact]
    public async Task Should_Write_Audit_Log_Besides_The_Table()
    {
        var user = AddPendingNutritionist();

        await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        AuditAssert.Recorded(_auditLogger, AdminId, AdminAuditAction.NutritionistPreRegistrationCancelled, user.Id);
    }
}
