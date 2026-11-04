using NogVita.Application.Abstractions;
using NogVita.Domain.Anamneses;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeQuestionnaireRepository : IQuestionnaireRepository
{
    public List<Questionnaire> Questionnaires { get; } = [];

    public Task<Questionnaire?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Questionnaires.FirstOrDefault(q => q.Id == id));

    public void Add(Questionnaire questionnaire) => Questionnaires.Add(questionnaire);

    public void Remove(Questionnaire questionnaire) => Questionnaires.Remove(questionnaire);
}
