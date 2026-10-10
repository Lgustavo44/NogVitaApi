using NogVita.Application.Abstractions;
using NogVita.Domain.FollowUps;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeNutritionistRequestRepository : INutritionistRequestRepository
{
    public List<NutritionistRequest> Requests { get; } = [];

    public Task<NutritionistRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Requests.FirstOrDefault(r => r.Id == id));

    public Task<bool> HasPendingForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Requests.Any(r => r.PatientId == patientId && r.IsPending));

    public void Add(NutritionistRequest request) => Requests.Add(request);
}