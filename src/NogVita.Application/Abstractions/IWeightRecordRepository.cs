using NogVita.Domain.WeightRecords;

namespace NogVita.Application.Abstractions;

public interface IWeightRecordRepository
{
    Task<WeightRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WeightRecord?> GetForPatientOnDateAsync(Guid patientId, DateOnly recordedOn, CancellationToken cancellationToken = default);

    void Add(WeightRecord record);

    void Remove(WeightRecord record);
}