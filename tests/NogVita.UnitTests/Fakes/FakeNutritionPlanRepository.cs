using NogVita.Application.Abstractions;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeNutritionPlanRepository : INutritionPlanRepository
{
    public List<NutritionPlan> Plans { get; } = [];

    public Task<NutritionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Plans.FirstOrDefault(p => p.Id == id));

    public Task<NutritionPlan?> GetActiveForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Plans.FirstOrDefault(p => p.PatientId == patientId && p.Status == NutritionPlanStatus.Active));

    public Task<IReadOnlyList<NutritionPlan>> ListOpenAsync(
        Guid nutritionistId,
        Guid patientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NutritionPlan>>(
            Plans.Where(p => p.NutritionistId == nutritionistId && p.PatientId == patientId && p.IsEditable).ToList());

    public void Add(NutritionPlan plan) => Plans.Add(plan);
}