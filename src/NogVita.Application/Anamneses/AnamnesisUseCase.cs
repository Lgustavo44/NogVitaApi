using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.FollowUps;
using NogVita.Application.Patients;
using NogVita.Domain.Anamneses;

namespace NogVita.Application.Anamneses;

public sealed record SendAnamnesisRequest(Guid QuestionnaireId);

public sealed class SendAnamnesisRequestValidator : AbstractValidator<SendAnamnesisRequest>
{
    public SendAnamnesisRequestValidator()
    {
        RuleFor(r => r.QuestionnaireId).NotEmpty().WithMessage("Escolha o questionário.");
    }
}

public sealed record SubmitAnamnesisRequest(IReadOnlyList<Answer> Answers);

public sealed class SubmitAnamnesisRequestValidator : AbstractValidator<SubmitAnamnesisRequest>
{
    public SubmitAnamnesisRequestValidator()
    {
        RuleFor(r => r.Answers).NotNull().WithMessage("Envie as respostas.");
        RuleForEach(r => r.Answers).NotNull().WithMessage("Resposta inválida.");
    }
}

public enum SendAnamnesisStatus
{
    Sent,
    PatientNotFound,
    QuestionnaireNotFound
}

public sealed record SendAnamnesisResult(SendAnamnesisStatus Status, Guid? AnamnesisId = null);

public enum SubmitAnamnesisResult
{
    Submitted,
    NotFound,
    AlreadySubmitted,
    NoLongerAvailable
}

public sealed class AnamnesisUseCase(
    CareAccessService careAccessService,
    IUserRepository userRepository,
    IQuestionnaireRepository questionnaireRepository,
    IAnamnesisRepository anamnesisRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    // D-V2-30: só o nutricionista que acompanha envia. A anamnese pendente ainda não tem dado de saúde,
    // então o termo (D-V2-22) só é cobrado na resposta.
    public async Task<SendAnamnesisResult> SendAsync(
        Guid nutritionistId, Guid patientId, SendAnamnesisRequest request, CancellationToken cancellationToken = default)
    {
        if (!await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, patientId, cancellationToken))
        {
            return new SendAnamnesisResult(SendAnamnesisStatus.PatientNotFound);
        }

        var questionnaire = await questionnaireRepository.GetByIdAsync(request.QuestionnaireId, cancellationToken);

        if (questionnaire is null || questionnaire.NutritionistId != nutritionistId)
        {
            return new SendAnamnesisResult(SendAnamnesisStatus.QuestionnaireNotFound);
        }

        var anamnesis = new Anamnesis(questionnaire, patientId);
        anamnesisRepository.Add(anamnesis);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new SendAnamnesisResult(SendAnamnesisStatus.Sent, anamnesis.Id);
    }

    public async Task<SubmitAnamnesisResult> SubmitAsync(
        Guid patientId, Guid anamnesisId, SubmitAnamnesisRequest request, CancellationToken cancellationToken = default)
    {
        var anamnesis = await anamnesisRepository.GetByIdAsync(anamnesisId, cancellationToken);

        if (anamnesis is null || anamnesis.PatientId != patientId)
        {
            return SubmitAnamnesisResult.NotFound;
        }

        // D-V2-29: depois de enviada, fica travada.
        if (anamnesis.Status == AnamnesisStatus.Submitted)
        {
            return SubmitAnamnesisResult.AlreadySubmitted;
        }

        // Sem o vínculo, ninguém leria as respostas (D-V2-06): não vale a pena guardar dado de saúde novo.
        if (!await careAccessService.NutritionistFollowsPatientAsync(anamnesis.NutritionistId, patientId, cancellationToken))
        {
            return SubmitAnamnesisResult.NoLongerAvailable;
        }

        var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);

        if (patient is null)
        {
            return SubmitAnamnesisResult.NotFound;
        }

        HealthDataConsentGuard.EnsureAccepted(patient);

        anamnesis.Submit(request.Answers, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return SubmitAnamnesisResult.Submitted;
    }
}
