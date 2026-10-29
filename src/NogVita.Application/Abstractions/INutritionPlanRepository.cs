using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.Abstractions;

public interface INutritionPlanRepository
{
    Task<NutritionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NutritionPlan?> GetActiveForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NutritionPlan>> ListOpenAsync(
        Guid nutritionistId,
        Guid patientId,
        CancellationToken cancellationToken = default);

    void Add(NutritionPlan plan);
}
