using NogVita.Domain.Anamneses;
using NogVita.Domain.Common;

namespace NogVita.UnitTests.Domain.Anamneses;

public class AnamnesisTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PatientId = Guid.NewGuid();

    private readonly Question _goal = Question.Create("Qual é o seu objetivo?", QuestionType.Text, true, null);
    private readonly Question _smokes = Question.Create("Fuma?", QuestionType.YesNo, false, null);
    private readonly Question _alcohol = Question.Create("Bebe álcool?", QuestionType.SingleChoice, false, ["Não", "Às vezes", "Sempre"]);
    private readonly Question _allergies = Question.Create("Alergias", QuestionType.MultipleChoice, false, ["Lactose", "Glúten", "Amendoim"]);

    private Anamnesis Pending() =>
        new(new Questionnaire(Guid.NewGuid(), "Anamnese inicial", [_goal, _smokes, _alcohol, _allergies]), PatientId);

    [Fact]
    public void Sending_copies_title_and_questions()
    {
        var questionnaire = new Questionnaire(Guid.NewGuid(), "Anamnese inicial", [_goal]);

        var anamnesis = new Anamnesis(questionnaire, PatientId);
        questionnaire.Update("Outro título", [_smokes]);

        Assert.Equal("Anamnese inicial", anamnesis.Title);
        Assert.Equal(_goal.Id, Assert.Single(anamnesis.Questions).Id);
        Assert.Equal(questionnaire.NutritionistId, anamnesis.NutritionistId);
        Assert.Equal(AnamnesisStatus.Pending, anamnesis.Status);
        Assert.Null(anamnesis.Answers);
    }

    [Fact]
    public void Submit_keeps_only_the_field_of_each_question_type()
    {
        var anamnesis = Pending();

        anamnesis.Submit(
        [
            new Answer(_goal.Id, Text: " Emagrecer ", YesNo: true),
            new Answer(_smokes.Id, YesNo: false),
            new Answer(_alcohol.Id, Options: ["Às vezes"]),
            new Answer(_allergies.Id, Options: ["Lactose", "Glúten"])
        ], Now);

        Assert.Equal(AnamnesisStatus.Submitted, anamnesis.Status);
        Assert.Equal(Now, anamnesis.SubmittedAtUtc);
        Assert.Equal(new Answer(_goal.Id, Text: "Emagrecer"), anamnesis.Answers![0]);
        Assert.False(anamnesis.Answers[1].YesNo);
        Assert.Equal(["Lactose", "Glúten"], anamnesis.Answers[3].Options!);
    }

    [Fact]
    public void Optional_questions_can_stay_blank()
    {
        var anamnesis = Pending();

        anamnesis.Submit([new Answer(_goal.Id, Text: "Emagrecer"), new Answer(_allergies.Id, Options: [])], Now);

        Assert.Equal(_goal.Id, Assert.Single(anamnesis.Answers!).QuestionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Required_question_left_blank_is_rejected(string? text) =>
        Assert.Throws<DomainException>(() => Pending().Submit([new Answer(_goal.Id, Text: text)], Now));

    [Fact]
    public void Text_over_the_limit_is_rejected() =>
        Assert.Throws<DomainException>(() => Pending().Submit(
            [new Answer(_goal.Id, Text: new string('a', Anamnesis.AnswerTextMaxLength + 1))], Now));

    [Fact]
    public void Single_choice_with_two_options_is_rejected() =>
        Assert.Throws<DomainException>(() => Pending().Submit(
            [new Answer(_goal.Id, Text: "Emagrecer"), new Answer(_alcohol.Id, Options: ["Não", "Sempre"])], Now));

    [Fact]
    public void Option_that_is_not_in_the_question_is_rejected() =>
        Assert.Throws<DomainException>(() => Pending().Submit(
            [new Answer(_goal.Id, Text: "Emagrecer"), new Answer(_allergies.Id, Options: ["Soja"])], Now));

    [Fact]
    public void Repeated_option_is_rejected() =>
        Assert.Throws<DomainException>(() => Pending().Submit(
            [new Answer(_goal.Id, Text: "Emagrecer"), new Answer(_allergies.Id, Options: ["Lactose", "Lactose"])], Now));

    [Fact]
    public void Answer_to_an_unknown_question_is_rejected() =>
        Assert.Throws<DomainException>(() => Pending().Submit(
            [new Answer(_goal.Id, Text: "Emagrecer"), new Answer(Guid.NewGuid(), Text: "?")], Now));

    [Fact]
    public void Two_answers_to_the_same_question_are_rejected() =>
        Assert.Throws<DomainException>(() => Pending().Submit(
            [new Answer(_goal.Id, Text: "Emagrecer"), new Answer(_goal.Id, Text: "Ganhar massa")], Now));

    [Fact]
    public void Submitting_twice_is_rejected()
    {
        var anamnesis = Pending();
        anamnesis.Submit([new Answer(_goal.Id, Text: "Emagrecer")], Now);

        Assert.Throws<DomainException>(() => anamnesis.Submit([new Answer(_goal.Id, Text: "Outra")], Now));
        Assert.Equal("Emagrecer", Assert.Single(anamnesis.Answers!).Text);
    }

    [Fact]
    public void Failed_submit_keeps_the_anamnesis_pending()
    {
        var anamnesis = Pending();

        Assert.Throws<DomainException>(() => anamnesis.Submit([], Now));

        Assert.Equal(AnamnesisStatus.Pending, anamnesis.Status);
        Assert.Null(anamnesis.Answers);
    }
}
