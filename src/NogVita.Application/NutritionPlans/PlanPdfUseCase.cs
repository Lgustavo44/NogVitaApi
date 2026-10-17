using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Application.NutritionPlans;

public sealed record PlanPdfFile(string FileName, byte[] Content);

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
    INutritionPlanPdfRenderer renderer,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<PlanPdfUseCase> logger)
{
    public async Task<PlanPdfFile?> RenderForPatientAsync(Guid patientId, Guid planId, CancellationToken cancellationToken = default)
    {
        var detail = await planQueries.GetByPatientAsync(patientId, planId, cancellationToken);

        return detail is null ? null : Render(detail);
    }

    public async Task<PlanPdfFile?> RenderForNutritionistAsync(Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default)
    {
        var detail = await nutritionistPlanQueries.GetAsync(nutritionistId, planId, cancellationToken);

        return detail is null ? null : Render(detail);
    }

    public async Task<EmailPlanPdfResult> EmailToPatientAsync(Guid patientId, Guid planId, CancellationToken cancellationToken = default)
    {
        var detail = await planQueries.GetByPatientAsync(patientId, planId, cancellationToken);
        var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);

        if (detail is null || patient is null)
        {
            return EmailPlanPdfResult.NotFound;
        }

        var file = Render(detail);
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

    private PlanPdfFile Render(NutritionPlanDetailResponse detail)
    {
        var content = renderer.Render(detail, BrazilianCalendar.Today(timeProvider));
        var fileName = $"nogvita-plano-{detail.Plan.StartDate:yyyy-MM-dd}.pdf";

        return new PlanPdfFile(fileName, content);
    }
}