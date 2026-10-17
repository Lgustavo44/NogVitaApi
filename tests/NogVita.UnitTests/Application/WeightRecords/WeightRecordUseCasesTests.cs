using NogVita.Application.FollowUps;
using NogVita.Application.WeightRecords;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;
using NogVita.Domain.WeightRecords;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.WeightRecords;

public class WeightRecordUseCasesTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FakeWeightRecordRepository _recordRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly User _patient;
    private readonly User _nutritionist;
    private readonly CareRelationship _relationship;

    public WeightRecordUseCasesTests()
    {
        _patient = new User("Paciente Teste", "paciente@email.com", Cpf.Create("11144477735"));
        _patient.SetPasswordHash("hash");
        _patient.ConfirmEmail(DateTime.UtcNow);
        _patient.CreatePatientProfile(new DateOnly(1995, 3, 10), BiologicalSex.Male, 178, Goal.MuscleGain);
        _userRepository.Add(_patient);

        _nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        _nutritionist.SetPasswordHash("hash");
        _nutritionist.ConfirmEmail(DateTime.UtcNow);
        _nutritionist.CreateNutritionistProfile(1, "123456");
        _nutritionist.NutritionistProfile!.Activate();
        _userRepository.Add(_nutritionist);

        _relationship = new NutritionistRequest(_patient.Id, _nutritionist.Id, null).Accept(DateTime.UtcNow);
        _relationshipRepository.Add(_relationship);
    }

    private CareAccessService CareAccess() => new(_userRepository, _relationshipRepository);

    private RecordWeightUseCase CreateRecordUseCase() =>
        new(_userRepository, CareAccess(), _recordRepository, _unitOfWork, TimeProvider.System);

    private DeleteWeightRecordUseCase CreateDeleteUseCase() =>
        new(_recordRepository, CareAccess(), _unitOfWork);

    [Fact]
    public async Task Patient_Should_Record_Today_By_Default()
    {
        var result = await CreateRecordUseCase().RecordForSelfAsync(
            _patient.Id, new RecordWeightRequest(80m, null, null), TestContext.Current.CancellationToken);

        Assert.Equal(RecordWeightStatus.Created, result.Status);
        var record = Assert.Single(_recordRepository.Records);
        Assert.Equal(record.Id, result.RecordId);
        Assert.True(record.WasRecordedByPatient);
    }

    [Fact]
    public async Task Same_Author_Same_Day_Should_Replace()
    {
        var useCase = CreateRecordUseCase();
        await useCase.RecordForSelfAsync(_patient.Id, new RecordWeightRequest(80m, null, null), TestContext.Current.CancellationToken);

        var second = await useCase.RecordForSelfAsync(
            _patient.Id, new RecordWeightRequest(79.5m, null, "Corrigido."), TestContext.Current.CancellationToken);

        Assert.Equal(RecordWeightStatus.Updated, second.Status);
        var record = Assert.Single(_recordRepository.Records);
        Assert.Equal(79.5m, record.WeightKg);
        Assert.Equal(2, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Patient_Should_Not_Replace_Nutritionist_Record_Of_The_Same_Day()
    {
        var useCase = CreateRecordUseCase();
        await useCase.RecordForPatientAsync(
            _nutritionist.Id, _patient.Id, new RecordWeightRequest(81m, null, "Consulta."), TestContext.Current.CancellationToken);

        var result = await useCase.RecordForSelfAsync(
            _patient.Id, new RecordWeightRequest(79m, null, null), TestContext.Current.CancellationToken);

        Assert.Equal(RecordWeightStatus.RecordedByOther, result.Status);
        Assert.Equal(81m, Assert.Single(_recordRepository.Records).WeightKg);
    }

    [Fact]
    public async Task Nutritionist_Without_Relationship_Should_Not_Record()
    {
        _relationship.End(_patient.Id, DateTime.UtcNow);

        var result = await CreateRecordUseCase().RecordForPatientAsync(
            _nutritionist.Id, _patient.Id, new RecordWeightRequest(80m, null, null), TestContext.Current.CancellationToken);

        Assert.Equal(RecordWeightStatus.NotFound, result.Status);
        Assert.Empty(_recordRepository.Records);
    }

    [Fact]
    public async Task Patient_Should_Delete_Own_Record()
    {
        await CreateRecordUseCase().RecordForSelfAsync(_patient.Id, new RecordWeightRequest(80m, null, null), TestContext.Current.CancellationToken);
        var record = _recordRepository.Records.Single();

        var deleted = await CreateDeleteUseCase().ExecuteAsync(_patient.Id, record.Id, TestContext.Current.CancellationToken);

        Assert.True(deleted);
        Assert.Empty(_recordRepository.Records);
    }

    [Fact]
    public async Task Patient_Should_Not_Delete_Nutritionist_Record()
    {
        await CreateRecordUseCase().RecordForPatientAsync(
            _nutritionist.Id, _patient.Id, new RecordWeightRequest(81m, null, null), TestContext.Current.CancellationToken);
        var record = _recordRepository.Records.Single();

        var deleted = await CreateDeleteUseCase().ExecuteAsync(_patient.Id, record.Id, TestContext.Current.CancellationToken);

        Assert.False(deleted);
        Assert.Single(_recordRepository.Records);
    }

    [Fact]
    public async Task Former_Nutritionist_Should_Not_Delete_Own_Old_Record()
    {
        await CreateRecordUseCase().RecordForPatientAsync(
            _nutritionist.Id, _patient.Id, new RecordWeightRequest(81m, null, null), TestContext.Current.CancellationToken);
        var record = _recordRepository.Records.Single();
        _relationship.End(_patient.Id, DateTime.UtcNow);

        var deleted = await CreateDeleteUseCase().ExecuteAsync(_nutritionist.Id, record.Id, TestContext.Current.CancellationToken);

        Assert.False(deleted);
        Assert.Single(_recordRepository.Records);
    }
}