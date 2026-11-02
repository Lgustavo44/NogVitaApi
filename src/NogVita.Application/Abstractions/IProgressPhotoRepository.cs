using NogVita.Domain.ProgressPhotos;

namespace NogVita.Application.Abstractions;

public interface IProgressPhotoRepository
{
    Task<ProgressPhoto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProgressPhoto?> GetAsync(Guid patientId, DateOnly takenOn, ProgressPhotoAngle angle, CancellationToken cancellationToken = default);

    Task<int> CountForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    // Para revogar o consentimento (D-V2-17): todas as fotos do paciente.
    Task<IReadOnlyList<ProgressPhoto>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    void Add(ProgressPhoto photo);

    void Remove(ProgressPhoto photo);

    void RemoveRange(IEnumerable<ProgressPhoto> photos);
}
