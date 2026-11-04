using NogVita.Domain.Anamneses;

namespace NogVita.Application.Abstractions;

public interface IQuestionnaireRepository
{
    Task<Questionnaire?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Questionnaire questionnaire);

    void Remove(Questionnaire questionnaire);
}
