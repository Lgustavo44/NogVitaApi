namespace NogVita.Application.NutritionPlans;

public interface INutritionPlanPdfRenderer
{
    byte[] Render(NutritionPlanDetailResponse plan, PlanPdfStamp stamp);
}

// O que identifica o documento (Res. CFN 760/2023, art. 10): o CRN, a data e a hora da emissão e o código verificador.
public sealed record PlanPdfStamp(
    string VerificationCode,
    DateTime IssuedAtBrasilia,
    int CrnRegion,
    string CrnNumber,
    string VerificationUrl);
