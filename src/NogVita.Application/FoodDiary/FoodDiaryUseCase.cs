using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.Foods;
using NogVita.Application.Patients;
using NogVita.Domain.FoodDiary;
using NogVita.Domain.Users;

namespace NogVita.Application.FoodDiary;

public sealed record CheckInMealRequest(MealAdherence Adherence, string? Notes);

public sealed class CheckInMealRequestValidator : AbstractValidator<CheckInMealRequest>
{
    public CheckInMealRequestValidator()
    {
        RuleFor(r => r.Adherence).IsInEnum().WithMessage("Diga se seguiu a refeição: Followed, Partial ou Skipped.");
        RuleFor(r => r.Notes)
            .MaximumLength(DiaryDay.NotesMaxLength)
            .WithMessage($"As observações podem ter no máximo {DiaryDay.NotesMaxLength} caracteres.");
    }
}

public sealed record AddExtraFoodRequest(
    Guid FoodId,
    Guid? MealId,
    decimal? QuantityInGrams,
    Guid? PortionId = null,
    decimal? PortionQuantity = null) : IFoodAmountRequest;

public sealed class AddExtraFoodRequestValidator : AbstractValidator<AddExtraFoodRequest>
{
    public AddExtraFoodRequestValidator()
    {
        RuleFor(r => r.FoodId).NotEmpty().WithMessage("O alimento é obrigatório.");
        this.AddFoodAmountRules();
    }
}

public enum FoodDiaryResult
{
    Done,
    PatientNotFound,
    NoActivePlan,
    MealNotFound,
    FoodNotFound,
    PortionNotFound,
    NotFound
}

public sealed record FoodDiaryOutcome(FoodDiaryResult Result, Guid? Id = null);

// D-V2-37: as escritas do diário, só do próprio paciente e só com o aceite do termo (D-V2-22).
public sealed class FoodDiaryUseCase(
    IUserRepository userRepository,
    IFoodDiaryRepository diaryRepository,
    INutritionPlanRepository planRepository,
    IFoodRepository foodRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<FoodDiaryOutcome> CheckInMealAsync(
        Guid patientId, DateOnly date, Guid mealId, CheckInMealRequest request, CancellationToken cancellationToken = default)
    {
        if (await LoadPatientAsync(patientId, cancellationToken) is null)
        {
            return new FoodDiaryOutcome(FoodDiaryResult.PatientNotFound);
        }

        var plan = await FoodDiaryReader.LoadActivePlanAsync(planRepository, patientId, cancellationToken);

        if (plan is null)
        {
            return new FoodDiaryOutcome(FoodDiaryResult.NoActivePlan);
        }

        var meal = plan.Meals.FirstOrDefault(m => m.Id == mealId);

        if (meal is null)
        {
            return new FoodDiaryOutcome(FoodDiaryResult.MealNotFound);
        }

        var today = BrazilianCalendar.Today(timeProvider);
        var day = await GetOrCreateDayAsync(patientId, date, today, cancellationToken);

        day.CheckIn(meal, FoodDiaryReader.PlannedOf(meal), request.Adherence, request.Notes, today);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new FoodDiaryOutcome(FoodDiaryResult.Done);
    }

    public async Task<FoodDiaryResult> ClearMealCheckInAsync(Guid patientId, DateOnly date, Guid mealId, CancellationToken cancellationToken = default)
    {
        if (await LoadPatientAsync(patientId, cancellationToken) is null)
        {
            return FoodDiaryResult.PatientNotFound;
        }

        var day = await diaryRepository.GetDayAsync(patientId, date, cancellationToken);

        if (day is null || !day.ClearCheckIn(mealId, BrazilianCalendar.Today(timeProvider)))
        {
            return FoodDiaryResult.NotFound;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return FoodDiaryResult.Done;
    }

    public async Task<FoodDiaryOutcome> AddExtraFoodAsync(
        Guid patientId, DateOnly date, AddExtraFoodRequest request, CancellationToken cancellationToken = default)
    {
        if (await LoadPatientAsync(patientId, cancellationToken) is null)
        {
            return new FoodDiaryOutcome(FoodDiaryResult.PatientNotFound);
        }

        // Num extra ligado a uma refeição, ela precisa ser do plano ativo (ou já ter um check-in nesse dia).
        if (request.MealId is { } mealId)
        {
            var plan = await FoodDiaryReader.LoadActivePlanAsync(planRepository, patientId, cancellationToken);
            var existing = await diaryRepository.GetDayAsync(patientId, date, cancellationToken);

            if (plan?.Meals.Any(m => m.Id == mealId) != true && existing?.MealCheckIns.Any(c => c.MealId == mealId) != true)
            {
                return new FoodDiaryOutcome(FoodDiaryResult.MealNotFound);
            }
        }

        var food = await foodRepository.GetByIdAsync(request.FoodId, cancellationToken);

        if (food is null || !food.IsActive)
        {
            return new FoodDiaryOutcome(FoodDiaryResult.FoodNotFound);
        }

        var quantity = FoodAmount.Resolve(food, request);

        if (quantity is null)
        {
            return new FoodDiaryOutcome(FoodDiaryResult.PortionNotFound);
        }

        var today = BrazilianCalendar.Today(timeProvider);
        var day = await GetOrCreateDayAsync(patientId, date, today, cancellationToken);

        var extra = day.AddExtra(food, quantity, request.MealId, today);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new FoodDiaryOutcome(FoodDiaryResult.Done, extra.Id);
    }

    public async Task<FoodDiaryResult> RemoveExtraFoodAsync(Guid patientId, DateOnly date, Guid extraId, CancellationToken cancellationToken = default)
    {
        if (await LoadPatientAsync(patientId, cancellationToken) is null)
        {
            return FoodDiaryResult.PatientNotFound;
        }

        var day = await diaryRepository.GetDayAsync(patientId, date, cancellationToken);

        if (day is null || !day.RemoveExtra(extraId, BrazilianCalendar.Today(timeProvider)))
        {
            return FoodDiaryResult.NotFound;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return FoodDiaryResult.Done;
    }

    private async Task<User?> LoadPatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);

        if (patient is null || !patient.IsActive || patient.PatientProfile is null)
        {
            return null;
        }

        HealthDataConsentGuard.EnsureAccepted(patient);
        return patient;
    }

    private async Task<DiaryDay> GetOrCreateDayAsync(Guid patientId, DateOnly date, DateOnly today, CancellationToken cancellationToken)
    {
        var day = await diaryRepository.GetDayAsync(patientId, date, cancellationToken);

        if (day is null)
        {
            day = new DiaryDay(patientId, date, today);
            diaryRepository.Add(day);
        }

        return day;
    }
}
