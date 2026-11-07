using NogVita.Domain.Common;

namespace NogVita.Domain.FoodDiary;

// D-V2-39: o check-in da semana. O paciente conta como foi (fome, energia, sono de 1 a 5, as dificuldades e um comentário),
// e o nutricionista que o acompanha escreve uma devolutiva. Depois da devolutiva, o check-in do paciente fica travado.
public class WeeklyCheckIn : Entity
{
    public const int MinScore = 1;
    public const int MaxScore = 5;
    public const int TextMaxLength = 1000;
    public const int MaxWeeksInPast = 4;

    public Guid PatientId { get; private set; }
    public Guid? NutritionistId { get; private set; }
    public DateOnly WeekStart { get; private set; }
    public int Hunger { get; private set; }
    public int Energy { get; private set; }
    public int Sleep { get; private set; }
    public string? Difficulties { get; private set; }
    public string? Comment { get; private set; }
    public string? Feedback { get; private set; }
    public DateTime? FeedbackAtUtc { get; private set; }

    public bool HasFeedback => Feedback is not null;

    private WeeklyCheckIn() { } // EF Core

    public WeeklyCheckIn(Guid patientId, Guid? nutritionistId, DateOnly weekStart, DateOnly today)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainException("A semana começa na segunda-feira.");
        }

        if (weekStart > today)
        {
            throw new DomainException("A semana ainda não começou.");
        }

        if (weekStart < WeekOf(today).AddDays(-7 * MaxWeeksInPast))
        {
            throw new DomainException($"O check-in só pode ser feito até {MaxWeeksInPast} semanas atrás.");
        }

        PatientId = patientId;
        NutritionistId = nutritionistId;
        WeekStart = weekStart;
    }

    public static DateOnly WeekOf(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public void Answer(int hunger, int energy, int sleep, string? difficulties, string? comment, Guid? nutritionistId)
    {
        if (HasFeedback)
        {
            throw new DomainException("O nutricionista já respondeu este check-in. Ele não pode mais ser alterado.");
        }

        Hunger = Score(hunger, "fome");
        Energy = Score(energy, "energia");
        Sleep = Score(sleep, "sono");
        Difficulties = Text(difficulties, "As dificuldades");
        Comment = Text(comment, "O comentário");
        NutritionistId = nutritionistId ?? NutritionistId;
    }

    public void GiveFeedback(Guid nutritionistId, string feedback, DateTime nowUtc)
    {
        Feedback = Text(feedback, "A devolutiva") ?? throw new DomainException("Escreva a devolutiva.");
        NutritionistId = nutritionistId;
        FeedbackAtUtc = nowUtc;
    }

    private static int Score(int value, string name) =>
        value is >= MinScore and <= MaxScore
            ? value
            : throw new DomainException($"A nota de {name} vai de {MinScore} a {MaxScore}.");

    private static string? Text(string? value, string name)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        return normalized is { Length: > TextMaxLength }
            ? throw new DomainException($"{name} pode ter no máximo {TextMaxLength} caracteres.")
            : normalized;
    }
}
