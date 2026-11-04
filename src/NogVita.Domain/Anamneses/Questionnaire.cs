using NogVita.Domain.Common;

namespace NogVita.Domain.Anamneses;

// D-V2-27: o questionário é do nutricionista. Editar ou apagar não mexe nas anamneses já enviadas (D-V2-31).
public class Questionnaire : Entity
{
    public const int TitleMaxLength = 100;
    public const int MaxQuestions = 50;

    public Guid NutritionistId { get; private set; }
    public string Title { get; private set; } = null!;
    public IReadOnlyList<Question> Questions { get; private set; } = [];

    private Questionnaire() { } // EF Core

    public Questionnaire(Guid nutritionistId, string title, IReadOnlyList<Question> questions)
    {
        NutritionistId = nutritionistId;
        Update(title, questions);
    }

    public void Update(string title, IReadOnlyList<Question> questions)
    {
        title = title?.Trim() ?? string.Empty;

        if (title.Length == 0 || title.Length > TitleMaxLength)
        {
            throw new DomainException($"O título do questionário precisa ter de 1 a {TitleMaxLength} caracteres.");
        }

        if (questions is null || questions.Count is 0 or > MaxQuestions)
        {
            throw new DomainException($"O questionário precisa ter de 1 a {MaxQuestions} perguntas.");
        }

        Title = title;
        Questions = questions.ToList();
    }
}
