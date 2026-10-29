using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.FollowUps;
using NogVita.Domain.WeightRecords;

namespace NogVita.Application.WeightRecords;

public sealed record RecordWeightRequest(decimal WeightKg, DateOnly? RecordedOn, string? Notes);

public sealed class RecordWeightRequestValidator : AbstractValidator<RecordWeightRequest>
{
    public RecordWeightRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(r => r.WeightKg).InclusiveBetween(WeightRecord.MinWeightKg, WeightRecord.MaxWeightKg);
        RuleFor(r => r.Notes).MaximumLength(WeightRecord.NotesMaxLength);

        RuleFor(r => r.RecordedOn)
            .Must(date =>
            {
                var today = BrazilianCalendar.Today(timeProvider);
                return date!.Value <= today && date.Value >= today.AddDays(-WeightRecord.MaxDaysInPast);
            })
            .When(r => r.RecordedOn is not null)
            .WithMessage($"A data precisa ser de hoje ou de até {WeightRecord.MaxDaysInPast} dias atrás.");
    }
}

public enum RecordWeightStatus
{
    Created,
    Updated,
    NotFound,
    RecordedByOther
}

public sealed record RecordWeightResult(RecordWeightStatus Status, Guid? RecordId = null);

public sealed class RecordWeightUseCase(
    IUserRepository userRepository,
    CareAccessService careAccessService,
    IWeightRecordRepository recordRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RecordWeightResult> RecordForSelfAsync(
        Guid patientId, RecordWeightRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);

        if (patient is null || !patient.IsActive || patient.PatientProfile is null)
        {
            return new RecordWeightResult(RecordWeightStatus.NotFound);
        }

        return await RecordAsync(patientId, authorId: patientId, request, cancellationToken);
    }

    public async Task<RecordWeightResult> RecordForPatientAsync(
        Guid nutritionistId, Guid patientId, RecordWeightRequest request, CancellationToken cancellationToken = default)
    {
        var followsPatient = await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, patientId, cancellationToken);

        if (!followsPatient)
        {
            return new RecordWeightResult(RecordWeightStatus.NotFound);
        }

        return await RecordAsync(patientId, authorId: nutritionistId, request, cancellationToken);
    }

    private async Task<RecordWeightResult> RecordAsync(
        Guid patientId, Guid authorId, RecordWeightRequest request, CancellationToken cancellationToken)
    {
        var today = BrazilianCalendar.Today(timeProvider);
        var recordedOn = request.RecordedOn ?? today;

        var existing = await recordRepository.GetForPatientOnDateAsync(patientId, recordedOn, cancellationToken);


        if (existing is not null && !existing.WasRecordedBy(authorId))
        {
            return new RecordWeightResult(RecordWeightStatus.RecordedByOther);
        }

        if (existing is not null)
        {
            existing.Update(authorId, request.WeightKg, request.Notes);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RecordWeightResult(RecordWeightStatus.Updated, existing.Id);
        }

        var record = new WeightRecord(patientId, authorId, recordedOn, request.WeightKg, request.Notes, today);

        recordRepository.Add(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecordWeightResult(RecordWeightStatus.Created, record.Id);

    }
}
