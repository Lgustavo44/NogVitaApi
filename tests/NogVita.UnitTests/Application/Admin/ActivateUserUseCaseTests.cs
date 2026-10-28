using NogVita.Application.Admin;
using NogVita.Domain.Audit;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Admin;

public class ActivateUserUseCaseTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CapturingLogger<AdminAuditLog> _auditLogger = new();

    private ActivateUserUseCase CreateUseCase() => new(_userRepository, _unitOfWork, new AdminAuditLog(_auditLogger));

    private User AddBlockedUser()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash("hash");
        user.ConfirmEmail(DateTime.UtcNow);
        user.Deactivate(DateTime.UtcNow);
        _userRepository.Add(user);
        return user;
    }

    [Fact]
    public async Task Should_Activate_And_Remove_Block()
    {
        var user = AddBlockedUser();

        var result = await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ActivateUserResult.Success, result);
        Assert.True(user.IsActive);
        Assert.False(user.IsBlockedByAdmin);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Audit_Activation()
    {
        var user = AddBlockedUser();

        await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        AuditAssert.Recorded(_auditLogger, AdminId, AdminAuditAction.UserActivated, user.Id);
    }

    [Fact]
    public async Task Should_Not_Activate_Nor_Audit_User_Without_Password()
    {
        var user = TestData.CreateUser();
        _userRepository.Add(user);

        var result = await CreateUseCase().ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ActivateUserResult.PasswordNotDefined, result);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
        AuditAssert.Nothing(_auditLogger);
    }

    [Fact]
    public async Task Should_Not_Audit_Unknown_User()
    {
        var result = await CreateUseCase().ExecuteAsync(AdminId, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(ActivateUserResult.NotFound, result);
        AuditAssert.Nothing(_auditLogger);
    }
}
