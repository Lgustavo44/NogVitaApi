using NogVita.Application.Anamneses;
using NogVita.Domain.Anamneses;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Anamneses;

public class ManageQuestionnaireUseCaseTests
{
    private static readonly Guid NutritionistId = Guid.NewGuid();
    private readonly FakeQuestionnaireRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private ManageQuestionnaireUseCase UseCase() => new(_repository, _unitOfWork);

    private static QuestionnaireRequest Request(string title = "Anamnese") =>
        new(title, [new QuestionRequest("Qual é o seu objetivo?", QuestionType.Text, true, null)]);

    [Fact]
    public async Task Should_Create_From_The_Base_Template()
    {
        var id = await UseCase().CreateAsync(NutritionistId, QuestionnaireTemplate.Base, _ct);

        var questionnaire = Assert.Single(_repository.Questionnaires);
        Assert.Equal(id, questionnaire.Id);
        Assert.Equal(QuestionnaireTemplate.Base.Questions.Count, questionnaire.Questions.Count);
    }

    [Fact]
    public void Base_Template_Should_Pass_The_Validator() =>
        Assert.True(new QuestionnaireRequestValidator().Validate(QuestionnaireTemplate.Base).IsValid);

    [Fact]
    public async Task Another_Nutritionist_Should_Not_Update_Or_Delete()
    {
        var id = await UseCase().CreateAsync(NutritionistId, Request(), _ct);
        var stranger = Guid.NewGuid();

        Assert.False(await UseCase().UpdateAsync(stranger, id, Request("Outro"), _ct));
        Assert.False(await UseCase().DeleteAsync(stranger, id, _ct));
        Assert.Equal("Anamnese", Assert.Single(_repository.Questionnaires).Title);
    }

    [Fact]
    public async Task Owner_Should_Update_And_Delete()
    {
        var id = await UseCase().CreateAsync(NutritionistId, Request(), _ct);

        Assert.True(await UseCase().UpdateAsync(NutritionistId, id, Request("Retorno"), _ct));
        Assert.Equal("Retorno", Assert.Single(_repository.Questionnaires).Title);

        Assert.True(await UseCase().DeleteAsync(NutritionistId, id, _ct));
        Assert.Empty(_repository.Questionnaires);
    }

    [Fact]
    public void Validator_Should_Reject_Options_On_A_Text_Question() =>
        Assert.False(new QuestionnaireRequestValidator().Validate(new QuestionnaireRequest("Anamnese", [new QuestionRequest("Pergunta", QuestionType.Text, false, ["Sim", "Não"])])).IsValid);

    [Fact]
    public void Validator_Should_Reject_A_Choice_Question_With_One_Option() =>
        Assert.False(new QuestionnaireRequestValidator().Validate(new QuestionnaireRequest("Anamnese", [new QuestionRequest("Pergunta", QuestionType.SingleChoice, false, ["Sim"])])).IsValid);
}
