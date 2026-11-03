using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.Abstractions;

public interface IPlanDocumentRepository
{
    Task<PlanDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    void Add(PlanDocument document);
}
