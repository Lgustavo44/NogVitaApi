using NogVita.Domain.Common;

namespace NogVita.Domain.Anamneses;

public enum AnamnesisStatus
{
    Pending,
    Submitted
}

// O questionário enviado a um paciente. Guarda o retrato do título e das perguntas (D-V2-31), como o FoodSnapshot
// faz com o alimento: as respostas ficam ligadas às perguntas que o paciente leu de verdade.
public class Anamnesis : Entity
{
    public const int AnswerTextMaxLength = 2000;

    public Guid NutritionistId { get; private set; }
    public Guid PatientId { get; private set; }
    public string Title { get; private set; } = null!;
    public IReadOnlyList<Question> Questions { get; private set; } = [];
    public IReadOnlyList<Answer>? Answers { get; private set; }
    public AnamnesisStatus Status { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }

    private Anamnesis() { } // EF Core

    public Anamnesis(Questionnaire questionnaire, Guid patientId)
    {
        ArgumentNullException.ThrowIfNull(questionnaire);

        NutritionistId = questionnaire.NutritionistId;
        PatientId = patientId;
        Title = questionnaire.Title;
        Questions = questionnaire.Questions.ToList();
        Status = AnamnesisStatus.Pending;
    }

    // D-V2-29: responde uma vez só. Para atualizar, o nutricionista envia outra anamnese.
    public void Submit(IReadOnlyList<Answer> answers, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(answers);

        if (Status == AnamnesisStatus.Submitted)
        {
            throw new DomainException("Esta anamnese já foi respondida.");
        }

        if (answers.Select(a => a.QuestionId).Distinct().Count() != answers.Count)
        {
            throw new DomainException("Cada pergunta pode ter uma resposta só.");
        }

        var byQuestion = answers.ToDictionary(a => a.QuestionId);

        if (byQuestion.Keys.Any(id => Questions.All(q => q.Id != id)))
        {
            throw new DomainException("Uma das respostas é de uma pergunta que não está nesta anamnese.");
        }

        var normalized = new List<Answer>();

        foreach (var question in Questions)
        {
            var answer = byQuestion.TryGetValue(question.Id, out var given) ? Normalize(question, given) : null;

            if (answer is null)
            {
                if (question.Required)
                {
                    throw new DomainException($"Responda a pergunta obrigatória \"{question.Text}\".");
                }

                continue;
            }

            normalized.Add(answer);
        }

        Answers = normalized;
        Status = AnamnesisStatus.Submitted;
        SubmittedAtUtc = nowUtc;
    }

    // Fica só o campo do tipo da pergunta. Uma resposta em branco vale como não respondida (null).
    private static Answer? Normalize(Question question, Answer answer)
    {
        switch (question.Type)
        {
            case QuestionType.Text:
                var text = answer.Text?.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    return null;
                }

                return text.Length > AnswerTextMaxLength
                    ? throw new DomainException($"A resposta pode ter no máximo {AnswerTextMaxLength} caracteres.")
                    : new Answer(question.Id, Text: text);

            case QuestionType.YesNo:
                return answer.YesNo is { } yesNo ? new Answer(question.Id, YesNo: yesNo) : null;

            default:
                var options = answer.Options ?? [];
                if (options.Count == 0)
                {
                    return null;
                }

                if (question.Type == QuestionType.SingleChoice && options.Count > 1)
                {
                    throw new DomainException($"Escolha uma opção só na pergunta \"{question.Text}\".");
                }

                if (options.Distinct().Count() != options.Count || options.Any(o => !question.Options.Contains(o)))
                {
                    throw new DomainException($"Escolha entre as opções da pergunta \"{question.Text}\".");
                }

                return new Answer(question.Id, Options: options.ToList());
        }
    }
}
