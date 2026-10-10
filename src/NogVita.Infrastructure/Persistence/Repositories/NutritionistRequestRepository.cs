using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.FollowUps;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class NutritionistRequestRepository(NogVitaDbContext context) : INutritionistRequestRepository
{
    public Task<NutritionistRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.NutritionistRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> HasPendingForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        context.NutritionistRequests.AnyAsync(
            r => r.PatientId == patientId && r.Status == NutritionistRequestStatus.Pending,
            cancellationToken);

    public void Add(NutritionistRequest request) => context.NutritionistRequests.Add(request);
}