using NogVita.Application.Abstractions;

namespace NogVita.Application.FollowUps;

public enum CancelNutritionistRequestResult
{
    Cancelled,
    NotFound,
    NotPending
}

public sealed class CancelNutritionistRequestUseCase(
    INutritionistRequestRepository requestRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<CancelNutritionistRequestResult> ExecuteAsync(
        Guid patientId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null || request.PatientId != patientId)
        {
            return CancelNutritionistRequestResult.NotFound;
        }
        if (!request.IsPending)
        {
            return CancelNutritionistRequestResult.NotPending;
        }
        request.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CancelNutritionistRequestResult.Cancelled;
    }
}