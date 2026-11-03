using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class PlanDocumentRepository(NogVitaDbContext context) : IPlanDocumentRepository
{
    public Task<PlanDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        context.PlanDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Code == code, cancellationToken);

    public void Add(PlanDocument document) => context.PlanDocuments.Add(document);
}
