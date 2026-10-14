using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common.Pagination;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.NutritionPlans;
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

        return Project(plans)
            .OrderByDescending(p => p.StartDate)
            .ThenBy(p => p.Id)
            .ToPagedResponseAsync(request, cancellationToken);
    }

    public Task<NutritionPlanResponse?> GetByNutritionistAsync(
        Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default) =>
        Project(context.NutritionPlans.AsNoTracking().Where(p => p.Id == planId && p.NutritionistId == nutritionistId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResponse<NutritionPlanResponse>> ListByPatientAsync(
        Guid patientId, NutritionPlanListRequest request, CancellationToken cancellationToken = default)
    {
        var plans = VisibleToPatient(patientId);

        if (request.Status is not null)
        {
            plans = plans.Where(p => p.Status == request.Status);
        }

        return Project(plans)
            .OrderByDescending(p => p.Status == NutritionPlanStatus.Active)
            .ThenByDescending(p => p.StartDate)
            .ThenBy(p => p.Id)
            .ToPagedResponseAsync(request, cancellationToken);
    }

    public Task<NutritionPlanResponse?> GetByPatientAsync(
        Guid patientId, Guid planId, CancellationToken cancellationToken = default) =>
        Project(VisibleToPatient(patientId).Where(p => p.Id == planId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<NutritionPlanResponse?> GetCurrentByPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default) =>
        Project(VisibleToPatient(patientId).Where(p => p.Status == NutritionPlanStatus.Active))
            .FirstOrDefaultAsync(cancellationToken);

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
            p.CreatedAt);
}