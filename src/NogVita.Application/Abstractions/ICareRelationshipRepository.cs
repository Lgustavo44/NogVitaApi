using NogVita.Domain.FollowUps;

namespace NogVita.Application.Abstractions;

public interface ICareRelationshipRepository
{
    Task<CareRelationship?> GetActiveForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CareRelationship>> ListActiveForNutritionistAsync(Guid nutritionistId, CancellationToken cancellationToken = default);
    void Add(CareRelationship relationship);
}
