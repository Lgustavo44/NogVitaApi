using NogVita.Application.Abstractions;
using NogVita.Domain.FollowUps;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeCareRelationshipRepository : ICareRelationshipRepository
{
    public List<CareRelationship> Relationships { get; } = [];

    public Task<CareRelationship?> GetActiveForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Relationships.FirstOrDefault(r => r.PatientId == patientId && r.IsActive));

    public Task<IReadOnlyList<CareRelationship>> ListActiveForNutritionistAsync(
        Guid nutritionistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CareRelationship>>(
            Relationships.Where(r => r.NutritionistId == nutritionistId && r.IsActive).ToList());

    public void Add(CareRelationship relationship) => Relationships.Add(relationship);
}