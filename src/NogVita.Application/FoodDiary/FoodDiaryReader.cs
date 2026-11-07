using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FoodDiary;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.FoodDiary;

// Sem datas: os últimos 7 dias, até hoje.
public sealed record DiaryRangeRequest
{
    public const int MaxDays = 31;

    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

public sealed class DiaryRangeRequestValidator : AbstractValidator<DiaryRangeRequest>
{
    public DiaryRangeRequestValidator()
    {
        RuleFor(r => r.To)
            .GreaterThanOrEqualTo(r => r.From).When(r => r.From is not null && r.To is not null)
            .WithMessage("A data final não pode ser anterior à inicial.");

        RuleFor(r => r)
            .Must(r => r.From is null || r.To is null || r.To.Value.DayNumber - r.From.Value.DayNumber < DiaryRangeRequest.MaxDays)
            .WithMessage($"Consulte no máximo {DiaryRangeRequest.MaxDays} dias de cada vez.")
            .OverridePropertyName(nameof(DiaryRangeRequest.From));
    }
}

public sealed class FoodDiaryReader(
    IFoodDiaryRepository diaryRepository,
    INutritionPlanRepository planRepository,
    CareAccessService careAccessService,
    TimeProvider timeProvider)
{
    public async Task<DiaryDayResponse> GetDayForPatientAsync(Guid patientId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var plan = await LoadActivePlanAsync(planRepository, patientId, cancellationToken);
        var day = await diaryRepository.GetDayAsync(patientId, date, cancellationToken);

        // D-V2-25: o paciente vê os macros do diário só se o plano permitir.
        return Build(date, day, plan, macrosVisible: plan?.PatientSeesMacros ?? true, includePendingMeals: true);
    }

    public async Task<DiaryRangeResponse> GetRangeForPatientAsync(Guid patientId, DiaryRangeRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await planRepository.GetActiveForPatientAsync(patientId, cancellationToken);
        return await RangeAsync(patientId, request, macrosVisible: plan?.PatientSeesMacros ?? true, cancellationToken);
    }

    // D-V2-06: só com o vínculo ativo. Sem ele, null (vira 404). O nutricionista vê sempre os macros.
    public async Task<DiaryRangeResponse?> GetRangeForNutritionistAsync(
        Guid nutritionistId, Guid patientId, DiaryRangeRequest request, CancellationToken cancellationToken = default) =>
        await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, patientId, cancellationToken)
            ? await RangeAsync(patientId, request, macrosVisible: true, cancellationToken)
            : null;

    // A adesão de cada paciente acompanhado nos últimos dias: os de menor adesão primeiro, e os sem registro no fim.
    public async Task<IReadOnlyList<PatientAdherenceResponse>> GetAdherenceForNutritionistAsync(
        Guid nutritionistId, int days, CancellationToken cancellationToken = default)
    {
        var to = BrazilianCalendar.Today(timeProvider);
        var from = to.AddDays(-(Math.Clamp(days, 1, DiaryRangeRequest.MaxDays) - 1));
        var patients = await diaryRepository.ListFollowedPatientsDiaryAsync(nutritionistId, from, to, cancellationToken);

        return patients
            .Select(p => new PatientAdherenceResponse(
                p.PatientId,
                p.PatientName,
                AdherenceOf(p.Days),
                p.Days.Count == 0 ? null : p.Days.Max(d => d.Date)))
            .OrderBy(p => p.Adherence.FollowedPercent is null)
            .ThenBy(p => p.Adherence.FollowedPercent)
            .ThenBy(p => p.PatientName)
            .ToList();
    }

    private static AdherenceResponse AdherenceOf(IEnumerable<DiaryDay> days)
    {
        var checkIns = days.SelectMany(d => d.MealCheckIns).ToList();

        return new AdherenceResponse(
            checkIns.Count(c => c.Adherence == MealAdherence.Followed),
            checkIns.Count(c => c.Adherence == MealAdherence.Partial),
            checkIns.Count(c => c.Adherence == MealAdherence.Skipped));
    }

    private async Task<DiaryRangeResponse> RangeAsync(Guid patientId, DiaryRangeRequest request, bool macrosVisible, CancellationToken cancellationToken)
    {
        var to = request.To ?? BrazilianCalendar.Today(timeProvider);
        var from = request.From ?? to.AddDays(-6);
        var days = await diaryRepository.ListDaysAsync(patientId, from, to, cancellationToken);

        // O histórico mostra só o que foi registrado: o plano de hoje não serve para dizer o que faltou responder num dia antigo.
        var responses = days
            .OrderByDescending(d => d.Date)
            .Select(d => Build(d.Date, d, plan: null, macrosVisible, includePendingMeals: false))
            .ToList();

        return new DiaryRangeResponse(from, to, responses, AdherenceOf(days));
    }

    // O plano ativo com as refeições e os alimentos (o GetActiveForPatientAsync não carrega as refeições).
    internal static async Task<NutritionPlan?> LoadActivePlanAsync(
        INutritionPlanRepository planRepository, Guid patientId, CancellationToken cancellationToken)
    {
        var active = await planRepository.GetActiveForPatientAsync(patientId, cancellationToken);
        return active is null ? null : await planRepository.GetByIdAsync(active.Id, cancellationToken);
    }

    // O planejado da refeição: só os itens principais, como nos totais do plano (D-V2-36).
    internal static NutrientAmounts PlannedOf(Meal meal)
    {
        var totals = NutrientTotals.FromPortions(meal.Foods
            .Where(f => !f.IsSubstitute)
            .Select(f => NutritionCalculator.Portion(f.Snapshot.Nutrients, f.QuantityInGrams)));

        return new NutrientAmounts(
            totals.EnergyKcal.Value, totals.Protein.Value, totals.Carbohydrate.Value, totals.Fat.Value, totals.Fiber.Value, totals.SodiumMg.Value);
    }

    internal static DiaryDayResponse Build(DateOnly date, DiaryDay? day, NutritionPlan? plan, bool macrosVisible, bool includePendingMeals)
    {
        var checkIns = day?.MealCheckIns ?? [];
        var extras = day?.ExtraFoods ?? [];
        var planMeals = includePendingMeals && plan is not null && plan.StartDate <= date
            ? plan.Meals.OrderBy(m => m.Order).ToList()
            : [];

        var meals = checkIns
            .Select(c => (c.MealId, c.MealName, c.Order, SuggestedTime: planMeals.FirstOrDefault(m => m.Id == c.MealId)?.SuggestedTime,
                Planned: c.Planned, Adherence: (MealAdherence?)c.Adherence, c.Notes))
            .Concat(planMeals
                .Where(m => checkIns.All(c => c.MealId != m.Id))
                .Select(m => (MealId: m.Id, MealName: m.Name, m.Order, m.SuggestedTime, Planned: PlannedOf(m), Adherence: (MealAdherence?)null, Notes: (string?)null)))
            .OrderBy(m => m.Order)
            .ToList();

        var mealResponses = meals
            .Select(m => new DiaryMealResponse(
                m.MealId, m.MealName, m.Order, m.SuggestedTime,
                NutrientTotalsResponse.From(NutrientTotals.FromPortions([m.Planned]), macrosVisible),
                m.Adherence, m.Notes,
                extras.Where(e => e.MealId == m.MealId).Select(e => ToResponse(e, macrosVisible)).ToList()))
            .ToList();

        var outside = extras
            .Where(e => e.MealId is null || meals.All(m => m.MealId != e.MealId))
            .Select(e => ToResponse(e, macrosVisible))
            .ToList();

        var planned = NutrientTotals.FromPortions(meals.Select(m => m.Planned));
        var consumed = NutrientTotals.FromPortions(meals
            .Where(m => m.Adherence == MealAdherence.Followed)
            .Select(m => m.Planned)
            .Concat(extras.Select(e => e.ComputeAmounts())));

        return new DiaryDayResponse(
            date,
            plan is not null,
            mealResponses,
            outside,
            NutrientTotalsResponse.From(planned, macrosVisible),
            NutrientTotalsResponse.From(consumed, macrosVisible),
            ConsumedIsComplete: meals.All(m => m.Adherence is MealAdherence.Followed or MealAdherence.Skipped),
            macrosVisible);
    }

    private static ExtraFoodResponse ToResponse(ExtraFood extra, bool macrosVisible)
    {
        var amounts = NutrientAmountsResponse.From(extra.ComputeAmounts());

        return new ExtraFoodResponse(
            extra.Id, extra.FoodId, extra.Name, extra.Brand, extra.Source, extra.QuantityInGrams,
            extra.PortionName is null ? null : new MeasureResponse(extra.PortionName, extra.PortionGrams!.Value, extra.PortionQuantity!.Value),
            macrosVisible ? amounts : amounts with { Protein = null, Carbohydrate = null, Fat = null });
    }
}
