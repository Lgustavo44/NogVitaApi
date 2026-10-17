using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common.Pagination;
using NogVita.Application.WeightRecords;
using NogVita.Infrastructure.Queries;

namespace NogVita.Infrastructure.Persistence.Queries;

internal sealed class WeightRecordQueries(NogVitaDbContext context) : IWeightRecordQueries
{
    public Task<PagedResponse<WeightRecordResponse>> ListByPatientAsync(
        Guid patientId, WeightHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var records = context.WeightRecords.AsNoTracking().Where(w => w.PatientId == patientId);

        if (request.From is not null)
        {
            records = records.Where(w => w.RecordedOn >= request.From);
        }

        if (request.To is not null)
        {
            records = records.Where(w => w.RecordedOn <= request.To);
        }

        var query =
            from w in records
            join author in context.Users on w.RecordedByUserId equals author.Id
            orderby w.RecordedOn descending
            select new WeightRecordResponse(
                w.Id,
                w.RecordedOn,
                w.WeightKg,
                w.Notes,
                w.RecordedByUserId == w.PatientId,
                author.Name);

        return query.ToPagedResponseAsync(request, cancellationToken);
    }

    public async Task<LatestWeightResponse?> GetLatestAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await context.WeightRecords
            .Where(record => record.PatientId == patientId)
            .OrderByDescending(record => record.RecordedOn)
            .Select(record => new LatestWeightResponse(record.WeightKg, record.RecordedOn))
            .FirstOrDefaultAsync(cancellationToken);
    }
}