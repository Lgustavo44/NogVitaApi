using NogVita.Domain.Common;

namespace NogVita.Domain.Anamneses;

// D-V2-28: os quatro tipos de pergunta.
public enum QuestionType
{
    Text,
    YesNo,
    SingleChoice,
    MultipleChoice
}

// Um valor, sem tabela própria: as perguntas vivem numa coluna jsonb do questionário e da anamnese (D-V2-32).
public sealed record Question(Guid Id, string Text, QuestionType Type, bool Required, IReadOnlyList<string> Options)
{
    public const int TextMaxLength = 500;
    public const int OptionMaxLength = 200;
    public const int MinOptions = 2;
    public const int MaxOptions = 20;

    // Um método, e não uma propriedade, para não ir parar no JSON.
    public static bool IsChoice(QuestionType type) => type is QuestionType.SingleChoice or QuestionType.MultipleChoice;

    public static Question Create(string text, QuestionType type, bool required, IReadOnlyList<string>? options)
    {
        text = text?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            throw new DomainException("O texto da pergunta é obrigatório.");
        }

        if (text.Length > TextMaxLength)
        {
            throw new DomainException($"O texto da pergunta pode ter no máximo {TextMaxLength} caracteres.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Tipo de pergunta inválido.");
        }

        return new Question(Guid.CreateVersion7(), text, type, required, IsChoice(type) ? ValidOptions(text, options) : NoOptions(options));
    }

    private static List<string> ValidOptions(string question, IReadOnlyList<string>? options)
    {
        var trimmed = (options ?? []).Select(o => o?.Trim() ?? string.Empty).ToList();

        if (trimmed.Count is < MinOptions or > MaxOptions)
        {
            throw new DomainException($"A pergunta \"{question}\" precisa ter de {MinOptions} a {MaxOptions} opções.");
        }

        if (trimmed.Any(o => o.Length == 0 || o.Length > OptionMaxLength))
        {
            throw new DomainException($"Cada opção precisa ter de 1 a {OptionMaxLength} caracteres.");
        }

        if (trimmed.Distinct(StringComparer.OrdinalIgnoreCase).Count() != trimmed.Count)
        {
            throw new DomainException($"As opções da pergunta \"{question}\" não podem se repetir.");
        }

        return trimmed;
    }

    private static List<string> NoOptions(IReadOnlyList<string>? options) =>
        options is { Count: > 0 }
            ? throw new DomainException("Só as perguntas de uma opção ou de várias opções têm opções.")
            : [];
}

// Só o campo do tipo da pergunta conta: Text, YesNo, ou Options (uma opção, ou várias), com os textos das opções.
public sealed record Answer(Guid QuestionId, string? Text = null, bool? YesNo = null, IReadOnlyList<string>? Options = null);
