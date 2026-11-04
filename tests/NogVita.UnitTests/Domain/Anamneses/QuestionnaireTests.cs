using NogVita.Domain.Anamneses;
using NogVita.Domain.Common;

namespace NogVita.UnitTests.Domain.Anamneses;

public class QuestionnaireTests
{
    private static readonly Guid NutritionistId = Guid.NewGuid();

    private static Question TextQuestion() => Question.Create("Qual é o seu objetivo?", QuestionType.Text, true, null);

    [Fact]
    public void Question_text_and_options_are_trimmed()
    {
        var question = Question.Create("  Bebe álcool? ", QuestionType.SingleChoice, false, [" Não ", "Às vezes"]);

        Assert.Equal("Bebe álcool?", question.Text);
        Assert.Equal(["Não", "Às vezes"], question.Options);
        Assert.NotEqual(Guid.Empty, question.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_question_text_is_rejected(string text) =>
        Assert.Throws<DomainException>(() => Question.Create(text, QuestionType.Text, false, null));

    [Fact]
    public void Question_text_over_the_limit_is_rejected() =>
        Assert.Throws<DomainException>(() => Question.Create(new string('a', Question.TextMaxLength + 1), QuestionType.Text, false, null));

    [Fact]
    public void Unknown_question_type_is_rejected() =>
        Assert.Throws<DomainException>(() => Question.Create("Pergunta", (QuestionType)99, false, null));

    [Theory]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.MultipleChoice)]
    public void Choice_question_needs_at_least_two_options(QuestionType type) =>
        Assert.Throws<DomainException>(() => Question.Create("Pergunta", type, false, ["Só uma"]));

    [Fact]
    public void Choice_question_over_the_option_limit_is_rejected() =>
        Assert.Throws<DomainException>(() => Question.Create("Pergunta", QuestionType.MultipleChoice, false,
            Enumerable.Range(1, Question.MaxOptions + 1).Select(i => $"Opção {i}").ToList()));

    [Fact]
    public void Repeated_options_are_rejected_ignoring_case() =>
        Assert.Throws<DomainException>(() => Question.Create("Pergunta", QuestionType.SingleChoice, false, ["Sim", "sim"]));

    [Fact]
    public void Blank_option_is_rejected() =>
        Assert.Throws<DomainException>(() => Question.Create("Pergunta", QuestionType.SingleChoice, false, ["Sim", " "]));

    [Theory]
    [InlineData(QuestionType.Text)]
    [InlineData(QuestionType.YesNo)]
    public void Options_on_a_non_choice_question_are_rejected(QuestionType type) =>
        Assert.Throws<DomainException>(() => Question.Create("Pergunta", type, false, ["Sim", "Não"]));

    [Fact]
    public void Questionnaire_keeps_title_and_questions()
    {
        var questionnaire = new Questionnaire(NutritionistId, " Anamnese inicial ", [TextQuestion()]);

        Assert.Equal("Anamnese inicial", questionnaire.Title);
        Assert.Single(questionnaire.Questions);
        Assert.Equal(NutritionistId, questionnaire.NutritionistId);
    }

    [Fact]
    public void Questionnaire_without_questions_is_rejected() =>
        Assert.Throws<DomainException>(() => new Questionnaire(NutritionistId, "Anamnese", []));

    [Fact]
    public void Questionnaire_over_the_question_limit_is_rejected() =>
        Assert.Throws<DomainException>(() => new Questionnaire(NutritionistId, "Anamnese",
            Enumerable.Range(0, Questionnaire.MaxQuestions + 1).Select(_ => TextQuestion()).ToList()));

    [Fact]
    public void Questionnaire_title_over_the_limit_is_rejected() =>
        Assert.Throws<DomainException>(() => new Questionnaire(NutritionistId, new string('a', Questionnaire.TitleMaxLength + 1), [TextQuestion()]));

    [Fact]
    public void Update_replaces_title_and_questions()
    {
        var questionnaire = new Questionnaire(NutritionistId, "Anamnese", [TextQuestion()]);

        questionnaire.Update("Retorno", [TextQuestion(), TextQuestion()]);

        Assert.Equal("Retorno", questionnaire.Title);
        Assert.Equal(2, questionnaire.Questions.Count);
    }
}
