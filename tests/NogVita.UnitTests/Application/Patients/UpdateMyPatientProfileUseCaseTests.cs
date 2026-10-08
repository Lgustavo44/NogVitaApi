using NogVita.Application.Patients;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Patients;

public class UpdateMyPatientProfileUseCaseTests
{
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));

    private static UpdatePatientProfileRequest CreateRequest() =>
        new(new DateOnly(1990, 7, 20), BiologicalSex.Female, 170, Goal.MuscleGain);

    private static User CreatePatient()
    {
        var user = new User("Maria Silva", "maria@email.com", Cpf.Create("52998224725"));
        user.SetPasswordHash("hash");
        user.Activate();
        user.CreatePatientProfile(new DateOnly(1995, 3, 10), BiologicalSex.Female, 165, Goal.WeightLoss);
        return user;
    }

    private UpdateMyPatientProfileUseCase CreateUseCase(User? user) =>
        new(new FakeUserRepository(user), _unitOfWork, _timeProvider);

    [Fact]
    public async Task Should_Update_Profile_And_Save()
    {
        var user = CreatePatient();
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(user.Id, CreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        var profile = user.PatientProfile!;
        Assert.Equal(new DateOnly(1990, 7, 20), profile.BirthDate);
        Assert.Equal(170, profile.HeightInCm);
        Assert.Equal(Goal.MuscleGain, profile.Goal);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Updated_Values_In_Response()
    {
        var user = CreatePatient();
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(user.Id, CreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(170, response.HeightInCm);
        Assert.Equal(Goal.MuscleGain, response.Goal);
        Assert.Equal(35, response.Age);
    }

    [Fact]
    public async Task Should_Return_Null_And_Not_Save_When_User_Has_No_Patient_Profile()
    {
        var user = new User("Admin", "admin@email.com", Cpf.Create("52998224725"));
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(user.Id, CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Null_And_Not_Save_For_Another_User_Id()
    {
        var user = CreatePatient();
        var useCase = CreateUseCase(user);

        var response = await useCase.ExecuteAsync(Guid.NewGuid(), CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}