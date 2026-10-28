using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;
using NogVita.Application.WeightRecords;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.FollowUps;

public class GetFollowedPatientUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly StubWeightQueries _weightQueries = new();

    private readonly User _patient;
    private readonly User _nutritionist;

    public GetFollowedPatientUseCaseTests()
    {
        _patient = new User("Paciente Teste", "paciente@email.com", Cpf.Create("11144477735"));
        _patient.CreatePatientProfile(new DateOnly(1995, 3, 10), BiologicalSex.Male, 178, Goal.MuscleGain);
        _patient.UpdatePatientRoutine(new TimeOnly(6, 0), new TimeOnly(22, 30), new TimeOnly(18, 0));
        _userRepository.Add(_patient);

        _nutritionist = TestUsers.Nutritionist();
        _userRepository.Add(_nutritionist);
    }

    private GetFollowedPatientUseCase CreateUseCase() =>
        new(new CareAccessService(_userRepository, _relationshipRepository), _userRepository, _weightQueries, _timeProvider);

    private CareRelationship Follow()
    {
        var relationship = TestUsers.Follow(_patient.Id, _nutritionist.Id);
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

    [Fact]
    public async Task Should_Have_No_Body_Mass_Index_Without_Weight()
    {
        Follow();

        var patient = await CreateUseCase().ExecuteAsync(_nutritionist.Id, _patient.Id, TestContext.Current.CancellationToken);

        Assert.Null(patient!.LatestWeight);
        Assert.Null(patient.BodyMassIndex);
    }

    [Fact]
    public async Task Should_Calculate_Body_Mass_Index_From_Latest_Weight()
    {
        Follow();
        _weightQueries.Latest = new LatestWeightResponse(80m, new DateOnly(2026, 10, 10));

        var patient = await CreateUseCase().ExecuteAsync(_nutritionist.Id, _patient.Id, TestContext.Current.CancellationToken);

        // 80 kg ÷ (1,78 m)² = 25,249... → 25,2
        Assert.Equal(80m, patient!.LatestWeight!.WeightKg);
        Assert.Equal(25.2m, patient.BodyMassIndex);
    }

    private sealed class StubWeightQueries : IWeightRecordQueries
    {
        public LatestWeightResponse? Latest { get; set; }

        public Task<LatestWeightResponse?> GetLatestAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Latest);

        public Task<PagedResponse<WeightRecordResponse>> ListByPatientAsync(
            Guid patientId, WeightHistoryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}