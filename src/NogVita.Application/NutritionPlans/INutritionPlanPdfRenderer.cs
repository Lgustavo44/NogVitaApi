using NogVita.Application.Documents;

namespace NogVita.Application.NutritionPlans;

public interface INutritionPlanPdfRenderer
{
    byte[] Render(NutritionPlanDetailResponse plan, DocumentStamp stamp);
}
