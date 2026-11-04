using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Domain.Anamneses;

namespace NogVita.Application.Anamneses;

public sealed record QuestionRequest(string Text, QuestionType Type, bool Required, IReadOnlyList<string>? Options);

public sealed record QuestionnaireRequest(string Title, IReadOnlyList<QuestionRequest> Questions);

// Espelha as regras do domínio, para o front receber 400 com todas as mensagens de uma vez.
// As opções repetidas ficam só no domínio (422).
public sealed class QuestionnaireRequestValidator : AbstractValidator<QuestionnaireRequest>
{
    public QuestionnaireRequestValidator()
    {
        RuleFor(r => r.Title)
            .NotEmpty().WithMessage("O título do questionário é obrigatório.")
            .MaximumLength(Questionnaire.TitleMaxLength).WithMessage($"O título pode ter no máximo {Questionnaire.TitleMaxLength} caracteres.");

        RuleFor(r => r.Questions)
            .NotEmpty().WithMessage("O questionário precisa ter pelo menos uma pergunta.")
            .Must(q => q is null || q.Count <= Questionnaire.MaxQuestions)
            .WithMessage($"O questionário pode ter no máximo {Questionnaire.MaxQuestions} perguntas.");

        RuleForEach(r => r.Questions)
            .NotNull().WithMessage("Pergunta inválida.")
            .ChildRules(question =>
            {
                question.RuleFor(q => q.Text)
                    .NotEmpty().WithMessage("O texto da pergunta é obrigatório.")
                    .MaximumLength(Question.TextMaxLength).WithMessage($"O texto da pergunta pode ter no máximo {Question.TextMaxLength} caracteres.");

                question.RuleFor(q => q.Type).IsInEnum().WithMessage("Tipo de pergunta inválido.");

                question.RuleFor(q => q.Options)
                    .Must(o => o is { Count: >= Question.MinOptions and <= Question.MaxOptions })
                    .When(q => Question.IsChoice(q.Type))
                    .WithMessage($"As perguntas de escolha precisam ter de {Question.MinOptions} a {Question.MaxOptions} opções.");

                question.RuleForEach(q => q.Options)
                    .NotEmpty().WithMessage("A opção não pode ficar em branco.")
                    .MaximumLength(Question.OptionMaxLength).WithMessage($"Cada opção pode ter no máximo {Question.OptionMaxLength} caracteres.")
                    .When(q => Question.IsChoice(q.Type));

                question.RuleFor(q => q.Options)
                    .Empty()
                    .When(q => !Question.IsChoice(q.Type))
                    .WithMessage("Só as perguntas de uma opção ou de várias opções têm opções.");
            });
    }
}

public sealed class ManageQuestionnaireUseCase(IQuestionnaireRepository questionnaireRepository, IUnitOfWork unitOfWork)
{
    public async Task<Guid> CreateAsync(
        Guid nutritionistId, QuestionnaireRequest request, CancellationToken cancellationToken = default)
    {
        var questionnaire = new Questionnaire(nutritionistId, request.Title, ToQuestions(request));

        questionnaireRepository.Add(questionnaire);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return questionnaire.Id;
    }

    // As perguntas são trocadas por inteiro (com Ids novos). As anamneses já enviadas têm o próprio retrato (D-V2-31).
    public async Task<bool> UpdateAsync(
        Guid nutritionistId, Guid questionnaireId, QuestionnaireRequest request, CancellationToken cancellationToken = default)
    {
        var questionnaire = await LoadOwnAsync(nutritionistId, questionnaireId, cancellationToken);

        if (questionnaire is null)
        {
            return false;
        }

        questionnaire.Update(request.Title, ToQuestions(request));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid nutritionistId, Guid questionnaireId, CancellationToken cancellationToken = default)
    {
        var questionnaire = await LoadOwnAsync(nutritionistId, questionnaireId, cancellationToken);

        if (questionnaire is null)
        {
            return false;
        }

        questionnaireRepository.Remove(questionnaire);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Questionnaire?> LoadOwnAsync(Guid nutritionistId, Guid questionnaireId, CancellationToken cancellationToken)
    {
        var questionnaire = await questionnaireRepository.GetByIdAsync(questionnaireId, cancellationToken);
        return questionnaire?.NutritionistId == nutritionistId ? questionnaire : null;
    }

    private static List<Question> ToQuestions(QuestionnaireRequest request) =>
        request.Questions.Select(q => Question.Create(q.Text, q.Type, q.Required, q.Options)).ToList();
}
