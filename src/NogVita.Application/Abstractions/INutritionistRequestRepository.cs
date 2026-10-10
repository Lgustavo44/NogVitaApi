using NogVita.Domain.FollowUps;

namespace NogVita.Application.Abstractions;

public interface INutritionistRequestRepository
{
    Task<NutritionistRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasPendingForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    void Add(NutritionistRequest request);
}