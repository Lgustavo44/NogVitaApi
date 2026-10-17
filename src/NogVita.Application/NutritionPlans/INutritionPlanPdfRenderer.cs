namespace NogVita.Application.NutritionPlans;

public interface INutritionPlanPdfRenderer
{
    byte[] Render(NutritionPlanDetailResponse plan, DateOnly generatedOn);
}