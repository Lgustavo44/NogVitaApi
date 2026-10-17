using NogVita.Application.Abstractions;
using NogVita.Domain.WeightRecords;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeWeightRecordRepository : IWeightRecordRepository
{
    public List<WeightRecord> Records { get; } = [];

    public Task<WeightRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Records.FirstOrDefault(r => r.Id == id));

    public Task<WeightRecord?> GetForPatientOnDateAsync(Guid patientId, DateOnly recordedOn, CancellationToken cancellationToken = default) =>
        Task.FromResult(Records.FirstOrDefault(r => r.PatientId == patientId && r.RecordedOn == recordedOn));

    public void Add(WeightRecord record) => Records.Add(record);

    public void Remove(WeightRecord record) => Records.Remove(record);
}