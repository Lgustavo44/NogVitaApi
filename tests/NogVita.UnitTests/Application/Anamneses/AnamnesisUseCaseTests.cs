using NogVita.Application.Anamneses;
using NogVita.Application.Patients;
using NogVita.Domain.Anamneses;
using NogVita.Domain.Common;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Anamneses;

public class AnamnesisUseCaseTests
{
    private readonly CareScenario _care = new();
    private readonly FakeQuestionnaireRepository _questionnaires = new();
    private readonly FakeAnamnesisRepository _anamneses = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Question _goal = Question.Create("Qual é o seu objetivo?", QuestionType.Text, true, null);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private AnamnesisUseCase UseCase() =>
        new(_care.CareAccess(), _care.Users, _questionnaires, _anamneses, _unitOfWork, TimeProvider.System);

    private Questionnaire NewQuestionnaire(Guid nutritionistId)
    {
        var questionnaire = new Questionnaire(nutritionistId, "Anamnese inicial", [_goal]);
        _questionnaires.Add(questionnaire);
        return questionnaire;
    }

    private Anamnesis Pending(Guid? patientId = null)
    {
        var anamnesis = new Anamnesis(NewQuestionnaire(_care.Nutritionist.Id), patientId ?? _care.Patient.Id);
        _anamneses.Add(anamnesis);
        return anamnesis;
    }

    private SubmitAnamnesisRequest Answers() => new([new Answer(_goal.Id, Text: "Emagrecer")]);

    [Fact]
    public async Task Nutritionist_Should_Send_Own_Questionnaire_To_Followed_Patient()
    {
        var questionnaire = NewQuestionnaire(_care.Nutritionist.Id);

        var result = await UseCase().SendAsync(_care.Nutritionist.Id, _care.Patient.Id, new(questionnaire.Id), _ct);

        Assert.Equal(SendAnamnesisStatus.Sent, result.Status);
        var anamnesis = Assert.Single(_anamneses.Anamneses);
        Assert.Equal(result.AnamnesisId, anamnesis.Id);
        Assert.Equal(_care.Patient.Id, anamnesis.PatientId);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Send_To_A_Patient_The_Nutritionist_Does_Not_Follow()
    {
        var stranger = _care.AnotherNutritionist();
        var questionnaire = NewQuestionnaire(stranger.Id);

        var result = await UseCase().SendAsync(stranger.Id, _care.Patient.Id, new(questionnaire.Id), _ct);

        Assert.Equal(SendAnamnesisStatus.PatientNotFound, result.Status);
        Assert.Empty(_anamneses.Anamneses);
    }

    [Fact]
    public async Task Should_Not_Send_Another_Nutritionists_Questionnaire()
    {
        var questionnaire = NewQuestionnaire(_care.AnotherNutritionist().Id);

        var result = await UseCase().SendAsync(_care.Nutritionist.Id, _care.Patient.Id, new(questionnaire.Id), _ct);

        Assert.Equal(SendAnamnesisStatus.QuestionnaireNotFound, result.Status);
        Assert.Empty(_anamneses.Anamneses);
    }

    [Fact]
    public async Task Patient_Should_Submit_Pending_Anamnesis()
    {
        var anamnesis = Pending();

        var result = await UseCase().SubmitAsync(_care.Patient.Id, anamnesis.Id, Answers(), _ct);

        Assert.Equal(SubmitAnamnesisResult.Submitted, result);
        Assert.Equal(AnamnesisStatus.Submitted, anamnesis.Status);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Another_Patient_Should_Not_Submit()
    {
        var anamnesis = Pending();
        var other = TestUsers.Patient(email: "outro@email.com").AddTo(_care.Users);

        var result = await UseCase().SubmitAsync(other.Id, anamnesis.Id, Answers(), _ct);

        Assert.Equal(SubmitAnamnesisResult.NotFound, result);
        Assert.Equal(AnamnesisStatus.Pending, anamnesis.Status);
    }

    [Fact]
    public async Task Submitted_Anamnesis_Should_Stay_Locked()
    {
        var anamnesis = Pending();
        var useCase = UseCase();
        await useCase.SubmitAsync(_care.Patient.Id, anamnesis.Id, Answers(), _ct);

        var result = await useCase.SubmitAsync(_care.Patient.Id, anamnesis.Id, new([new Answer(_goal.Id, Text: "Outra")]), _ct);

        Assert.Equal(SubmitAnamnesisResult.AlreadySubmitted, result);
        Assert.Equal("Emagrecer", Assert.Single(anamnesis.Answers!).Text);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Submit_After_The_Care_Relationship_Ended()
    {
        var anamnesis = Pending();
        _care.Relationship!.End(_care.Patient.Id, DateTime.UtcNow);

        var result = await UseCase().SubmitAsync(_care.Patient.Id, anamnesis.Id, Answers(), _ct);

        Assert.Equal(SubmitAnamnesisResult.NoLongerAvailable, result);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    // D-V2-22: as respostas são dado de saúde novo.
    [Fact]
    public async Task Patient_Without_Health_Data_Consent_Should_Not_Submit()
    {
        var patient = TestUsers.Patient(email: "antigo@email.com", healthDataConsent: false).AddTo(_care.Users);
        _care.Relationships.Add(TestUsers.Follow(patient.Id, _care.Nutritionist.Id));
        var anamnesis = Pending(patient.Id);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            UseCase().SubmitAsync(patient.Id, anamnesis.Id, Answers(), _ct));

        Assert.Equal(HealthDataConsentMessages.ConsentRequired, exception.Message);
        Assert.Equal(AnamnesisStatus.Pending, anamnesis.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}
