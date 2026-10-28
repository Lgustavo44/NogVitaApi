using NogVita.Domain.Audit;
using NogVita.Application.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.Nutritionists;
using NogVita.Domain.Invitations;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Nutritionists;

public class ResendNutritionistInvitationUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeInvitationRepository _invitationRepository = new();
    private readonly FakeSecureTokenService _secureTokenService = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private static readonly Guid AdminId = Guid.NewGuid();
    private readonly CapturingLogger<AdminAuditLog> _auditLogger = new();
    private AdminAuditLog AuditLog => new(_auditLogger);

    private ResendNutritionistInvitationUseCase CreateUseCase()
    {
        var invitationService = new NutritionistInvitationService(
            _invitationRepository,
            _secureTokenService,
            _emailSender,
            new FrontendSettings { BaseUrl = "http://localhost:5173" },
            TimeProvider.System,
            NullLogger<NutritionistInvitationService>.Instance);

        return new ResendNutritionistInvitationUseCase(
            _userRepository,
            _invitationRepository,
            _unitOfWork,
            invitationService,
            TimeProvider.System,
            AuditLog);
    }

    private User CreatePendingNutritionist()
    {
        var user = TestUsers.PendingNutritionist("Ana Lima", "ana@nogvita.local");
        _userRepository.Add(user);
        return user;
    }

    private NutritionistInvitation AddOldInvitation(User user)
    {
        var now = DateTime.UtcNow;
        var invitation = new NutritionistInvitation(user.Id, "hash-do-convite-antigo", now.AddHours(72), now);
        _invitationRepository.Add(invitation);
        return invitation;
    }

    [Fact]
    public async Task Should_Revoke_Previous_And_Send_New_Invitation()
    {
        var user = CreatePendingNutritionist();
        var oldInvitation = AddOldInvitation(user);
        var useCase = CreateUseCase();

        var status = await useCase.ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ResendInvitationStatus.Sent, status);
        Assert.True(oldInvitation.IsRevoked);
        Assert.Equal(2, _invitationRepository.Invitations.Count);

        var newInvitation = Assert.Single(_invitationRepository.Invitations, i => !i.IsRevoked);
        Assert.NotEqual(oldInvitation.Id, newInvitation.Id);

        Assert.Single(_emailSender.SentMessages);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Ask_New_User_To_Define_Password()
    {
        var user = CreatePendingNutritionist();
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Contains("defina sua senha", email.TextBody);
    }

    [Fact]
    public async Task Should_Keep_New_Invitation_When_Email_Fails()
    {
        var user = CreatePendingNutritionist();
        _emailSender.ShouldFail = true;
        var useCase = CreateUseCase();

        var status = await useCase.ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ResendInvitationStatus.EmailFailed, status);
        Assert.Single(_invitationRepository.Invitations);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Already_Active_When_Profile_Is_Active()
    {
        var user = CreatePendingNutritionist();
        user.NutritionistProfile!.Activate();
        var useCase = CreateUseCase();

        var status = await useCase.ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ResendInvitationStatus.AlreadyActive, status);
        Assert.Empty(_invitationRepository.Invitations);
        Assert.Empty(_emailSender.SentMessages);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Not_Found_For_User_Without_Nutritionist_Profile()
    {
        var user = new User("João Souza", "joao@nogvita.local", Cpf.Create("529.982.247-25"));
        _userRepository.Add(user);
        var useCase = CreateUseCase();

        var status = await useCase.ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ResendInvitationStatus.NotFound, status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Not_Found_For_Unknown_User()
    {
        var useCase = CreateUseCase();

        var status = await useCase.ExecuteAsync(AdminId, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(ResendInvitationStatus.NotFound, status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
        AuditAssert.Nothing(_auditLogger);
    }

    [Fact]
    public async Task Should_Audit_Resent_Invitation()
    {
        var user = CreatePendingNutritionist();

        await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        AuditAssert.Recorded(_auditLogger, AdminId, AdminAuditAction.NutritionistInvitationResent, user.Id);
    }

    [Fact]
    public async Task Should_Not_Audit_When_Invitation_Was_Already_Accepted()
    {
        var user = CreatePendingNutritionist();
        user.NutritionistProfile!.Activate();

        await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        AuditAssert.Nothing(_auditLogger);
    }
}