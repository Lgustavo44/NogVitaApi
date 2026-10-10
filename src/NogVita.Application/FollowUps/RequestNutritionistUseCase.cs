using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Domain.FollowUps;

namespace NogVita.Application.FollowUps;

public sealed record RequestNutritionistRequest(Guid NutritionistId, string? Message);

public sealed class RequestNutritionistRequestValidator : AbstractValidator<RequestNutritionistRequest>
{
    public RequestNutritionistRequestValidator()
    {
        RuleFor(r => r.NutritionistId).NotEmpty();
        RuleFor(r => r.Message).MaximumLength(NutritionistRequest.MessageMaxLength);
    }
}

public enum RequestNutritionistStatus
{
    Created,
    PatientNotFound,
    CannotRequestSelf,
    NutritionistNotFound,
    AlreadyHasNutritionist,
    PendingRequestExists
}

public sealed record RequestNutritionistResult(RequestNutritionistStatus Status, Guid? RequestId = null);

public sealed class RequestNutritionistUseCase(
    IUserRepository userRepository,
    INutritionistRequestRepository requestRepository,
    ICareRelationshipRepository relationshipRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<RequestNutritionistResult> ExecuteAsync(
        Guid patientId,
        RequestNutritionistRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);

        if (patient is null || !patient.IsActive || patient.PatientProfile is null)
        {
            return new RequestNutritionistResult(RequestNutritionistStatus.PatientNotFound);
        }

        if (request.NutritionistId == patientId)
        {
            return new RequestNutritionistResult(RequestNutritionistStatus.CannotRequestSelf);
        }

        var nutritionist = await userRepository.GetByIdAsync(request.NutritionistId, cancellationToken);

        if (nutritionist is null || !nutritionist.IsActive || nutritionist.NutritionistProfile is not { IsActive: true })
        {
            return new RequestNutritionistResult(RequestNutritionistStatus.NutritionistNotFound);
        }

        var activeRelationship = await relationshipRepository.GetActiveForPatientAsync(patientId, cancellationToken);

        if (activeRelationship is not null)
        {
            return new RequestNutritionistResult(RequestNutritionistStatus.AlreadyHasNutritionist);
        }

        var hasPendingRequest = await requestRepository.HasPendingForPatientAsync(patientId, cancellationToken);

        if (hasPendingRequest)
        {
            return new RequestNutritionistResult(RequestNutritionistStatus.PendingRequestExists);
        }

        var nutritionistRequest = new NutritionistRequest(patientId, request.NutritionistId, request.Message);
        requestRepository.Add(nutritionistRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RequestNutritionistResult(RequestNutritionistStatus.Created, nutritionistRequest.Id);
    }
}