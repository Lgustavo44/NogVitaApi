using NogVita.Application.Nutritionists;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Nutritionists;

public class MyNutritionistProfileUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private User AddActiveNutritionist()
    {
        var user = new User("Nutri Ativa", "nutri@email.com", Cpf.Create("12345678909"));
        user.SetPasswordHash("hash");
        user.ConfirmEmail(DateTime.UtcNow);
        user.CreateNutritionistProfile(1, "123456");
        user.NutritionistProfile!.Activate();
        _userRepository.Add(user);
        return user;
    }

    [Fact]
    public async Task Should_Return_Own_Profile()
    {
        var user = AddActiveNutritionist();

        var response = await new GetMyNutritionistProfileUseCase(_userRepository)
            .ExecuteAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(user.Id, response.UserId);
        Assert.Equal(1, response.CrnRegion);
    }

    [Fact]
    public async Task Should_Return_Null_When_User_Is_Not_Nutritionist()
    {
        var user = new User("Só Paciente", "paciente@email.com", Cpf.Create("11144477735"));
        _userRepository.Add(user);

        var response = await new GetMyNutritionistProfileUseCase(_userRepository)
            .ExecuteAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task Should_Update_Bio()
    {
        var user = AddActiveNutritionist();
        var useCase = new UpdateMyNutritionistProfileUseCase(_userRepository, _unitOfWork);

        var response = await useCase.ExecuteAsync(
            user.Id,
            new UpdateMyNutritionistProfileRequest("Nutrição esportiva."),
            TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal("Nutrição esportiva.", response.Bio);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Update_When_User_Does_Not_Exist()
    {
        var useCase = new UpdateMyNutritionistProfileUseCase(_userRepository, _unitOfWork);

        var response = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new UpdateMyNutritionistProfileRequest("Bio"),
            TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}