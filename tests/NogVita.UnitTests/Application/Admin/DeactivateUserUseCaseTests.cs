using NogVita.Application.Admin;
using NogVita.Domain.Auth;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Admin;

public class DeactivateUserUseCaseTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private static User CreateActiveUser()
    {
        var user = new User("João Souza", "joao@email.com", Cpf.Create("12345678909"));
        user.SetPasswordHash("hash");
        user.Activate();
        return user;
    }

    private DeactivateUserUseCase CreateUseCase(User? user) =>
        new(new FakeUserRepository(user), _refreshTokenRepository, _unitOfWork, TimeProvider.System);

    [Fact]
    public async Task Should_Deactivate_User_And_Revoke_All_Sessions()
    {
        var user = CreateActiveUser();
        var session = new RefreshToken(user.Id, "hash-da-sessao", DateTime.UtcNow.AddDays(7));
        _refreshTokenRepository.Add(session);
        var useCase = CreateUseCase(user);

        var result = await useCase.ExecuteAsync(AdminId, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(DeactivateUserResult.Success, result);
        Assert.False(user.IsActive);
        Assert.True(session.IsRevoked);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Deactivate_Own_Account()
    {
        var admin = CreateActiveUser();
        var useCase = CreateUseCase(admin);

        var result = await useCase.ExecuteAsync(admin.Id, admin.Id, TestContext.Current.CancellationToken);

        Assert.Equal(DeactivateUserResult.CannotDeactivateSelf, result);
        Assert.True(admin.IsActive);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Not_Found_For_Unknown_User()
    {
        var useCase = CreateUseCase(null);

        var result = await useCase.ExecuteAsync(AdminId, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(DeactivateUserResult.NotFound, result);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}