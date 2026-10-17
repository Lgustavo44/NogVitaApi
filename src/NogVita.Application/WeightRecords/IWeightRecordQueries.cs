using FluentValidation;
using NogVita.Application.Common.Pagination;

namespace NogVita.Application.WeightRecords;

public sealed record WeightHistoryRequest : PageRequest
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

public sealed class WeightHistoryRequestValidator : PageRequestValidator<WeightHistoryRequest>
{
    public WeightHistoryRequestValidator()
    {
        RuleFor(r => r.To)
            .GreaterThanOrEqualTo(r => r.From)
            .When(r => r.From is not null && r.To is not null)
            .WithMessage("A data final não pode ser anterior à inicial.");
    }
}

public sealed record WeightRecordResponse(
    Guid Id,
    DateOnly RecordedOn,
    decimal WeightKg,
    string? Notes,
    bool RecordedByPatient,
    string RecordedByName);

public sealed record LatestWeightResponse(decimal WeightKg, DateOnly RecordedOn);

public interface IWeightRecordQueries
{
    Task<PagedResponse<WeightRecordResponse>> ListByPatientAsync(
        Guid patientId, WeightHistoryRequest request, CancellationToken cancellationToken = default);

    Task<LatestWeightResponse?> GetLatestAsync(Guid patientId, CancellationToken cancellationToken = default);
}