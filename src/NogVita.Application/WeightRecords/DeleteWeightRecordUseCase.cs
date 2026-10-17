using NogVita.Application.Abstractions;
using NogVita.Application.FollowUps;

namespace NogVita.Application.WeightRecords;

public sealed class DeleteWeightRecordUseCase(
    IWeightRecordRepository recordRepository,
    CareAccessService careAccessService,
    IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(Guid userId, Guid recordId, CancellationToken cancellationToken = default)
    {
        var record = await recordRepository.GetByIdAsync(recordId, cancellationToken);

        if (record is null || !record.WasRecordedBy(userId))
        {
            return false;
        }

        var isPatientOwnRecord = record.PatientId == userId;

        if (!isPatientOwnRecord
            && !await careAccessService.NutritionistFollowsPatientAsync(userId, record.PatientId, cancellationToken))
        {
            return false;
        }

        recordRepository.Remove(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}