using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common.Pagination;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.Infrastructure.Queries;

namespace NogVita.Infrastructure.Persistence.Queries;

internal sealed class NutritionPlanQueries(NogVitaDbContext context) : INutritionPlanQueries
{
    public Task<PagedResponse<NutritionPlanResponse>> ListByNutritionistAndPatientAsync(
        Guid nutritionistId, Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default)
    {
        var plans = context.NutritionPlans
            .AsNoTracking()
            .Where(p => p.NutritionistId == nutritionistId && p.PatientId == patientId);

        if (request.Status is not null)
        {
            plans = plans.Where(p => p.Status == request.Status);
        }

        return Project(plans
                .OrderByDescending(p => p.StartDate)
                .ThenBy(p => p.Id))
            .ToPagedResponseAsync(request, cancellationToken);
    }

    public Task<NutritionPlanDetailResponse?> GetByNutritionistAsync(
        Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default) =>
        WithMealsAsync(
            context.NutritionPlans.AsNoTracking().Where(p => p.Id == planId && p.NutritionistId == nutritionistId),
            cancellationToken);

    public Task<PagedResponse<NutritionPlanResponse>> ListByPatientAsync(
        Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default)
    {
        var plans = VisibleToPatient(patientId);

        if (request.Status is not null)
        {
            plans = plans.Where(p => p.Status == request.Status);
        }

        return Project(plans
                .OrderByDescending(p => p.Status == NutritionPlanStatus.Active)
                .ThenByDescending(p => p.StartDate)
                .ThenBy(p => p.Id))
            .ToPagedResponseAsync(request, cancellationToken);
    }

    public Task<NutritionPlanDetailResponse?> GetByPatientAsync(
        Guid patientId, Guid planId, CancellationToken cancellationToken = default) =>
        WithMealsAsync(VisibleToPatient(patientId).Where(p => p.Id == planId), cancellationToken);

    public Task<NutritionPlanDetailResponse?> GetCurrentByPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default) =>
        WithMealsAsync(VisibleToPatient(patientId).Where(p => p.Status == NutritionPlanStatus.Active), cancellationToken);

    private IQueryable<NutritionPlan> VisibleToPatient(Guid patientId) =>
        context.NutritionPlans
            .AsNoTracking()
            .Where(p => p.PatientId == patientId
                && (p.Status == NutritionPlanStatus.Active || p.Status == NutritionPlanStatus.Finished));

    private IQueryable<NutritionPlanResponse> Project(IQueryable<NutritionPlan> plans) =>
        from p in plans
        join patient in context.Users on p.PatientId equals patient.Id
        join nutritionist in context.Users on p.NutritionistId equals nutritionist.Id
        select new NutritionPlanResponse(
            p.Id,
            p.PatientId,
            patient.Name,
            p.NutritionistId,
            nutritionist.Name,
            p.Name,
            p.Description,
            p.StartDate,
            p.EndDate,
            p.Status,
            p.CreatedAt,
            p.TargetEnergyKcal,
            p.TargetProteinGrams,
            p.TargetCarbohydrateGrams,
            p.TargetFatGrams);

    // Duas consultas: o plano (com os nomes do paciente e do nutricionista) e, se ele existir, as refeições em ordem.
    private async Task<NutritionPlanDetailResponse?> WithMealsAsync(
      IQueryable<NutritionPlan> plans,
      CancellationToken cancellationToken)
    {
        var plan = await Project(plans).FirstOrDefaultAsync(cancellationToken);

        if (plan is null)
        {
            return null;
        }

        var meals = await context.Set<Meal>()
            .AsNoTracking()
            .Where(m => m.NutritionPlanId == plan.Id)
            .OrderBy(m => m.Order)
            .Select(m => new { m.Id, m.Name, m.Notes, m.Order, m.SuggestedTime })
            .ToListAsync(cancellationToken);

        var mealIds = meals.Select(m => m.Id).ToList();

        var foods = await context.Set<MealFood>()
            .AsNoTracking()
            .Where(f => mealIds.Contains(f.MealId))
            .OrderBy(f => f.CreatedAt)
            .Select(f => new
            {
                f.MealId,
                Item = new MealFoodResponse(
                    f.Id,
                    f.FoodId,
                    f.QuantityInGrams,
                    f.Snapshot.Name,
                    f.Snapshot.Brand,
                    f.Snapshot.Source,
                    f.Snapshot.SourceReference,
                    f.Snapshot.TakenAtUtc,
                    f.Snapshot.Nutrients.EnergyKcal,
                    f.Snapshot.Nutrients.Protein,
                    f.Snapshot.Nutrients.Carbohydrate,
                    f.Snapshot.Nutrients.Fat,
                    f.Snapshot.Nutrients.Fiber,
                    f.Snapshot.Nutrients.SodiumMg)
            })
            .ToListAsync(cancellationToken);

        var foodsByMeal = foods.ToLookup(f => f.MealId, f => f.Item);

        var mealResponses = meals
            .Select(m => new MealResponse(m.Id, m.Name, m.Notes, m.Order, m.SuggestedTime, foodsByMeal[m.Id].ToList()))
            .ToList();

        return new NutritionPlanDetailResponse(plan, mealResponses);
    }
}