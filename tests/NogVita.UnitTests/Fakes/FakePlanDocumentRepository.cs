using NogVita.Application.Abstractions;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Fakes;

public sealed class FakePlanDocumentRepository : IPlanDocumentRepository
{
    public List<PlanDocument> Documents { get; } = [];

    public Task<PlanDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(Documents.FirstOrDefault(d => d.Code == code));

    public void Add(PlanDocument document) => Documents.Add(document);
}
