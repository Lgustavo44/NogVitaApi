using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.ProgressPhotos;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class ProgressPhotoRepository(NogVitaDbContext context) : IProgressPhotoRepository
{
    public Task<ProgressPhoto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.ProgressPhotos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<ProgressPhoto?> GetAsync(Guid patientId, DateOnly takenOn, ProgressPhotoAngle angle, CancellationToken cancellationToken = default) =>
        context.ProgressPhotos.FirstOrDefaultAsync(
            p => p.PatientId == patientId && p.TakenOn == takenOn && p.Angle == angle, cancellationToken);

    public Task<int> CountForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        context.ProgressPhotos.CountAsync(p => p.PatientId == patientId, cancellationToken);

    public async Task<IReadOnlyList<ProgressPhoto>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await context.ProgressPhotos.Where(p => p.PatientId == patientId).ToListAsync(cancellationToken);

    public void Add(ProgressPhoto photo) => context.ProgressPhotos.Add(photo);

    public void Remove(ProgressPhoto photo) => context.ProgressPhotos.Remove(photo);

    public void RemoveRange(IEnumerable<ProgressPhoto> photos) => context.ProgressPhotos.RemoveRange(photos);
}
