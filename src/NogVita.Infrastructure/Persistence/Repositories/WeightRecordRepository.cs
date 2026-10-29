using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.WeightRecords;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class WeightRecordRepository(NogVitaDbContext context) : IWeightRecordRepository
{
    public Task<WeightRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.WeightRecords.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<WeightRecord?> GetForPatientOnDateAsync(Guid patientId, DateOnly recordedOn, CancellationToken cancellationToken = default) =>
        context.WeightRecords.FirstOrDefaultAsync(w => w.PatientId == patientId && w.RecordedOn == recordedOn, cancellationToken);

    public void Add(WeightRecord record) => context.WeightRecords.Add(record);

    public void Remove(WeightRecord record) => context.WeightRecords.Remove(record);
}
