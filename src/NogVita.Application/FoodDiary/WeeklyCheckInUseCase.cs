using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.FollowUps;
using NogVita.Application.Patients;
using NogVita.Domain.FoodDiary;

namespace NogVita.Application.FoodDiary;

public sealed record WeeklyCheckInRequest(int Hunger, int Energy, int Sleep, string? Difficulties, string? Comment);

public sealed class WeeklyCheckInRequestValidator : AbstractValidator<WeeklyCheckInRequest>
{
    public WeeklyCheckInRequestValidator()
    {
        var scoreMessage = $"Dê uma nota de {WeeklyCheckIn.MinScore} a {WeeklyCheckIn.MaxScore}.";

        RuleFor(r => r.Hunger).InclusiveBetween(WeeklyCheckIn.MinScore, WeeklyCheckIn.MaxScore).WithMessage(scoreMessage);
        RuleFor(r => r.Energy).InclusiveBetween(WeeklyCheckIn.MinScore, WeeklyCheckIn.MaxScore).WithMessage(scoreMessage);
        RuleFor(r => r.Sleep).InclusiveBetween(WeeklyCheckIn.MinScore, WeeklyCheckIn.MaxScore).WithMessage(scoreMessage);
        RuleFor(r => r.Difficulties).MaximumLength(WeeklyCheckIn.TextMaxLength)
            .WithMessage($"As dificuldades podem ter no máximo {WeeklyCheckIn.TextMaxLength} caracteres.");
        RuleFor(r => r.Comment).MaximumLength(WeeklyCheckIn.TextMaxLength)
            .WithMessage($"O comentário pode ter no máximo {WeeklyCheckIn.TextMaxLength} caracteres.");
    }
}

public sealed record WeeklyFeedbackRequest(string Feedback);

public sealed class WeeklyFeedbackRequestValidator : AbstractValidator<WeeklyFeedbackRequest>
{
    public WeeklyFeedbackRequestValidator()
    {
        RuleFor(r => r.Feedback)
            .NotEmpty().WithMessage("Escreva a devolutiva.")
            .MaximumLength(WeeklyCheckIn.TextMaxLength).WithMessage($"A devolutiva pode ter no máximo {WeeklyCheckIn.TextMaxLength} caracteres.");
    }
}

public enum WeeklyCheckInResult
{
    Done,
    PatientNotFound,
    NotFound
}

public sealed record WeeklyCheckInOutcome(WeeklyCheckInResult Result, WeeklyCheckInResponse? CheckIn = null);

// D-V2-39: o paciente conta como foi a semana, e o nutricionista que o acompanha responde.
public sealed class WeeklyCheckInUseCase(
    IUserRepository userRepository,
    IFoodDiaryRepository diaryRepository,
    ICareRelationshipRepository relationshipRepository,
    CareAccessService careAccessService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public const int HistoryWeeks = 12;

    public async Task<WeeklyCheckInOutcome> SaveForPatientAsync(
        Guid patientId, DateOnly weekStart, WeeklyCheckInRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);

        if (patient is null || !patient.IsActive || patient.PatientProfile is null)
        {
            return new WeeklyCheckInOutcome(WeeklyCheckInResult.PatientNotFound);
        }

        HealthDataConsentGuard.EnsureAccepted(patient);

        // A devolutiva é de quem acompanha o paciente agora.
        var nutritionistId = (await relationshipRepository.GetActiveForPatientAsync(patientId, cancellationToken))?.NutritionistId;
        var checkIn = await diaryRepository.GetWeekAsync(patientId, weekStart, cancellationToken);

        if (checkIn is null)
        {
            checkIn = new WeeklyCheckIn(patientId, nutritionistId, weekStart, BrazilianCalendar.Today(timeProvider));
            diaryRepository.Add(checkIn);
        }

        checkIn.Answer(request.Hunger, request.Energy, request.Sleep, request.Difficulties, request.Comment, nutritionistId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WeeklyCheckInOutcome(WeeklyCheckInResult.Done, WeeklyCheckInResponse.From(checkIn));
    }

    public async Task<IReadOnlyList<WeeklyCheckInResponse>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        (await diaryRepository.ListWeeksAsync(patientId, HistoryWeeks, cancellationToken)).Select(WeeklyCheckInResponse.From).ToList();

    // D-V2-06: só com o vínculo ativo; sem ele, null (404).
    public async Task<IReadOnlyList<WeeklyCheckInResponse>?> ListForNutritionistAsync(
        Guid nutritionistId, Guid patientId, CancellationToken cancellationToken = default) =>
        await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, patientId, cancellationToken)
            ? await ListForPatientAsync(patientId, cancellationToken)
            : null;

    public async Task<IReadOnlyList<AwaitingFeedbackResponse>> ListAwaitingFeedbackAsync(Guid nutritionistId, CancellationToken cancellationToken = default) =>
        (await diaryRepository.ListAwaitingFeedbackAsync(nutritionistId, cancellationToken))
            .Select(x => new AwaitingFeedbackResponse(x.CheckIn.Id, x.CheckIn.PatientId, x.PatientName, x.CheckIn.WeekStart, x.CheckIn.UpdatedAt ?? x.CheckIn.CreatedAt))
            .ToList();

    public async Task<WeeklyCheckInOutcome> GiveFeedbackAsync(
        Guid nutritionistId, Guid checkInId, WeeklyFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var checkIn = await diaryRepository.GetWeekByIdAsync(checkInId, cancellationToken);

        if (checkIn is null || !await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, checkIn.PatientId, cancellationToken))
        {
            return new WeeklyCheckInOutcome(WeeklyCheckInResult.NotFound);
        }

        checkIn.GiveFeedback(nutritionistId, request.Feedback, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WeeklyCheckInOutcome(WeeklyCheckInResult.Done, WeeklyCheckInResponse.From(checkIn));
    }
}
