using NogVita.Application.FollowUps;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.FollowUps;

public class GetFollowedPatientUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));

    private readonly User _patient;
    private readonly User _nutritionist;

    public GetFollowedPatientUseCaseTests()
    {
        _patient = new User("Paciente Teste", "paciente@email.com", Cpf.Create("11144477735"));
        _patient.CreatePatientProfile(new DateOnly(1995, 3, 10), BiologicalSex.Male, 178, Goal.MuscleGain);
        _patient.UpdatePatientRoutine(new TimeOnly(6, 0), new TimeOnly(22, 30), new TimeOnly(18, 0));
        _userRepository.Add(_patient);

        _nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        _nutritionist.SetPasswordHash("hash");
        _nutritionist.ConfirmEmail(DateTime.UtcNow);
        _nutritionist.CreateNutritionistProfile(1, "123456");
        _nutritionist.NutritionistProfile!.Activate();
        _userRepository.Add(_nutritionist);
    }

    private GetFollowedPatientUseCase CreateUseCase() =>
        new(new CareAccessService(_userRepository, _relationshipRepository), _userRepository, _timeProvider);

    private CareRelationship Follow()
    {
        var relationship = new NutritionistRequest(_patient.Id, _nutritionist.Id, null).Accept(DateTime.UtcNow);
        _relationshipRepository.Add(relationship);
        return relationship;
    }

    [Fact]
    public async Task Should_Return_Profile_And_Routine_While_Following()
    {
        Follow();

        var patient = await CreateUseCase().ExecuteAsync(_nutritionist.Id, _patient.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(patient);
        Assert.Equal("Paciente Teste", patient.Name);
        Assert.Equal(31, patient.Age);
        Assert.Equal(BiologicalSex.Male, patient.BiologicalSex);
        Assert.Equal(178, patient.HeightInCm);
        Assert.Equal(Goal.MuscleGain, patient.Goal);
        Assert.Equal(new TimeOnly(6, 0), patient.WakeUpTime);
        Assert.Equal(new TimeOnly(22, 30), patient.BedTime);
        Assert.Equal(new TimeOnly(18, 0), patient.TrainingTime);
    }

    [Fact]
    public async Task Should_Hide_Patient_Not_Followed()
    {
        var patient = await CreateUseCase().ExecuteAsync(_nutritionist.Id, _patient.Id, TestContext.Current.CancellationToken);

        Assert.Null(patient);
    }

    [Fact]
    public async Task Should_Hide_Patient_After_Relationship_Ended()
    {
        var relationship = Follow();
        relationship.End(_patient.Id, DateTime.UtcNow);

        var patient = await CreateUseCase().ExecuteAsync(_nutritionist.Id, _patient.Id, TestContext.Current.CancellationToken);

        Assert.Null(patient);
    }

    [Fact]
    public async Task Should_Hide_Patient_Followed_By_Another_Nutritionist()
    {
        Follow();

        var patient = await CreateUseCase().ExecuteAsync(Guid.NewGuid(), _patient.Id, TestContext.Current.CancellationToken);

        Assert.Null(patient);
    }
}
