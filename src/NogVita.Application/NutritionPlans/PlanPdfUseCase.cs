using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Documents;
using NogVita.Domain.Documents;

namespace NogVita.Application.NutritionPlans;

public enum EmailPlanPdfResult
{
    Sent,
    NotFound,
    EmailFailed
}

public sealed class PlanPdfUseCase(
    INutritionPlanQueries planQueries,
    NutritionistPlanQueriesUseCase nutritionistPlanQueries,
    IUserRepository userRepository,
    DocumentIssuer documentIssuer,
    INutritionPlanPdfRenderer renderer,
    IEmailSender emailSender,
    ILogger<PlanPdfUseCase> logger)
{
    public async Task<PdfFile?> RenderForPatientAsync(Guid patientId, Guid planId, CancellationToken cancellationToken = default)
    {
        var detail = await planQueries.GetByPatientAsync(patientId, planId, cancellationToken);

        return detail is null ? null : await IssueAsync(detail, cancellationToken);
    }

    public async Task<PdfFile?> RenderForNutritionistAsync(Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default)
    {
        var detail = await nutritionistPlanQueries.GetAsync(nutritionistId, planId, cancellationToken);

        return detail is null ? null : await IssueAsync(detail, cancellationToken);
    }

    public async Task<EmailPlanPdfResult> EmailToPatientAsync(Guid patientId, Guid planId, CancellationToken cancellationToken = default)
    {
        var detail = await planQueries.GetByPatientAsync(patientId, planId, cancellationToken);
        var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);

        if (detail is null || patient is null)
        {
            return EmailPlanPdfResult.NotFound;
        }

        var file = await IssueAsync(detail, cancellationToken);
        var message = PlanPdfEmail.Create(patient.Email, patient.Name, detail.Plan.Name, file.FileName, file.Content);

        try
        {
            await emailSender.SendAsync(message, cancellationToken);
            return EmailPlanPdfResult.Sent;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao enviar o plano {PlanId} por e-mail.", planId);
            return EmailPlanPdfResult.EmailFailed;
        }
    }

    private async Task<PdfFile> IssueAsync(NutritionPlanDetailResponse detail, CancellationToken cancellationToken)
    {
        var content = await documentIssuer.IssueAsync(
            DocumentKind.NutritionPlan, detail.Plan.Id, detail.Plan.NutritionistId, detail.Plan.NutritionistName,
            stamp => renderer.Render(detail, stamp), cancellationToken);

        return new PdfFile($"nogvita-plano-{detail.Plan.StartDate:yyyy-MM-dd}.pdf", content);
    }
}
