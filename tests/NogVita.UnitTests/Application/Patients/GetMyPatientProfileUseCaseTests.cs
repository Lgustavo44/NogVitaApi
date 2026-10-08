using NogVita.Application.Patients;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Patients;

public class GetMyPatientProfileUseCaseTests
{
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));

    private static User CreatePatient()
    {
        var user = new User("Maria Silva", "maria@email.com", Cpf.Create("52998224725"));
        user.SetPasswordHash("hash");
        user.Activate();
        user.CreatePatientProfile(new DateOnly(1995, 3, 10), BiologicalSex.Female, 165, Goal.WeightLoss);
        return user;
    }

    [Fact]
    public async Task Should_Return_Null_When_User_Is_Inactive()
    {
        var user = CreatePatient();
        user.Deactivate();
        var useCase = new GetMyPatientProfileUseCase(new FakeUserRepository(user), _timeProvider);

        var response = await useCase.ExecuteAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task Should_Return_Profile_With_Age_And_Formatted_Cpf()
    {
        var user = CreatePatient();
        var useCase = new GetMyPatientProfileUseCase(new FakeUserRepository(user), _timeProvider);

        var response = await useCase.ExecuteAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(31, response.Age);
        Assert.Equal("529.982.247-25", response.Cpf);
    }

    [Fact]
    public async Task Should_Return_Null_When_User_Has_No_Patient_Profile()
    {
        var user = new User("Admin", "admin@email.com", Cpf.Create("52998224725"));
        var useCase = new GetMyPatientProfileUseCase(new FakeUserRepository(user), _timeProvider);

        var response = await useCase.ExecuteAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task Should_Return_Null_For_Another_User_Id()
    {
        var user = CreatePatient();
        var useCase = new GetMyPatientProfileUseCase(new FakeUserRepository(user), _timeProvider);

        var response = await useCase.ExecuteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Null(response);
    }
}