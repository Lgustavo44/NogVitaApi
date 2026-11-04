using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Anamneses;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class QuestionnaireRepository(NogVitaDbContext context) : IQuestionnaireRepository
{
    public Task<Questionnaire?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Questionnaires.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public void Add(Questionnaire questionnaire) => context.Questionnaires.Add(questionnaire);

    public void Remove(Questionnaire questionnaire) => context.Questionnaires.Remove(questionnaire);
}
