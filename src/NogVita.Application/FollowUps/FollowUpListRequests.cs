using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;

namespace NogVita.Application.FollowUps;


public sealed record NutritionistRequestListRequest : PageRequest
{
    public NutritionistRequestStatus? Status { get; init; }
}

public sealed class NutritionistRequestListRequestValidator : PageRequestValidator<NutritionistRequestListRequest>
{
    public NutritionistRequestListRequestValidator()
    {
        RuleFor(r => r.Status).IsInEnum().When(r => r.Status is not null);
    }
}

public sealed record MyPatientsListRequest : PageRequest
{
    public string? Search { get; init; }
}

public sealed class MyPatientsListRequestValidator : PageRequestValidator<MyPatientsListRequest>
{
    public MyPatientsListRequestValidator()
    {
        RuleFor(r => r.Search).MaximumLength(100);
    }
}

public sealed record PatientRequestItem(
    Guid Id,
    Guid NutritionistId,
    string NutritionistName,
    NutritionistRequestStatus Status,
    string? Message,
    DateTime CreatedAt,
    DateTime? RespondedAtUtc);

public sealed record NutritionistRequestItem(
    Guid Id,
    Guid PatientId,
    string PatientName,
    NutritionistRequestStatus Status,
    string? Message,
    DateTime CreatedAt,
    DateTime? RespondedAtUtc);

public sealed record MyPatientItem(
    Guid PatientId,
    string Name,
    Goal Goal,
    DateTime StartedAtUtc);

public enum RespondNutritionistRequestResult
{
    Done,
    NotFound,
    NotPending
}

public sealed class RespondNutritionistRequestUseCase(
    INutritionistRequestRepository requestRepository,
    ICareRelationshipRepository relationshipRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RespondNutritionistRequestResult> AcceptAsync(
        Guid nutritionistId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var (request, result) = await LoadOwnPendingAsync(nutritionistId, requestId, cancellationToken);

        if (request is null)
        {
            return result;
        }

        var relationship = request.Accept(timeProvider.GetUtcNow().UtcDateTime);
        relationshipRepository.Add(relationship);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RespondNutritionistRequestResult.Done;
    }

    public async Task<RespondNutritionistRequestResult> RejectAsync(
        Guid nutritionistId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var (request, result) = await LoadOwnPendingAsync(nutritionistId, requestId, cancellationToken);

        if (request is null)
        {
            return result;
        }

        request.Reject(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RespondNutritionistRequestResult.Done;
    }

    private async Task<(NutritionistRequest? Request, RespondNutritionistRequestResult Result)> LoadOwnPendingAsync(
        Guid nutritionistId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var nutritionist = await userRepository.GetByIdAsync(nutritionistId, cancellationToken);

        if (nutritionist is null || !nutritionist.IsActive || nutritionist.NutritionistProfile is not { IsActive: true })
        {
            return (null, RespondNutritionistRequestResult.NotFound);
        }

        var request = await requestRepository.GetByIdAsync(requestId, cancellationToken);

        if (request is null || request.NutritionistId != nutritionistId)
        {
            return (null, RespondNutritionistRequestResult.NotFound);
        }

        if (!request.IsPending)
        {
            return (null, RespondNutritionistRequestResult.NotPending);
        }

        return (request, RespondNutritionistRequestResult.Done);
    }
}