using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.FollowUps;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class CareRelationshipRepository(NogVitaDbContext context) : ICareRelationshipRepository
{
    public Task<CareRelationship?> GetActiveForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        context.CareRelationships.FirstOrDefaultAsync(
            r => r.PatientId == patientId && r.EndedAtUtc == null,
            cancellationToken);

    public void Add(CareRelationship relationship) => context.CareRelationships.Add(relationship);
}