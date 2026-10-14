using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class NutritionPlanRepository(NogVitaDbContext context) : INutritionPlanRepository
{
    public Task<NutritionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.NutritionPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<NutritionPlan?> GetActiveForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        context.NutritionPlans.FirstOrDefaultAsync(p => p.PatientId == patientId && p.Status == NutritionPlanStatus.Active, cancellationToken)
        ;

    public async Task<IReadOnlyList<NutritionPlan>> ListOpenAsync(
        Guid nutritionistId,
        Guid patientId,
        CancellationToken cancellationToken = default) =>
        await context.NutritionPlans.Where(p => p.NutritionistId == nutritionistId && p.PatientId == patientId && (p.Status == NutritionPlanStatus.Draft || p.Status == NutritionPlanStatus.Active)).ToListAsync(cancellationToken);
        

    public void Add(NutritionPlan plan) => context.NutritionPlans.Add(plan);
}