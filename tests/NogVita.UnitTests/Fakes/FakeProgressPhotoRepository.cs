using NogVita.Application.Abstractions;
using NogVita.Domain.ProgressPhotos;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeProgressPhotoRepository : IProgressPhotoRepository
{
    public List<ProgressPhoto> Photos { get; } = [];

    public Task<ProgressPhoto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Photos.FirstOrDefault(p => p.Id == id));

    public Task<ProgressPhoto?> GetAsync(Guid patientId, DateOnly takenOn, ProgressPhotoAngle angle, CancellationToken cancellationToken = default) =>
        Task.FromResult(Photos.FirstOrDefault(p => p.PatientId == patientId && p.TakenOn == takenOn && p.Angle == angle));

    public Task<int> CountForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Photos.Count(p => p.PatientId == patientId));

    public Task<IReadOnlyList<ProgressPhoto>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProgressPhoto>>(Photos.Where(p => p.PatientId == patientId).ToList());

    public void Add(ProgressPhoto photo) => Photos.Add(photo);

    public void Remove(ProgressPhoto photo) => Photos.Remove(photo);

    public void RemoveRange(IEnumerable<ProgressPhoto> photos)
    {
        foreach (var photo in photos.ToList())
        {
            Photos.Remove(photo);
        }
    }
}
