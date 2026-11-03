using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

public sealed record PlanPdfFile(string FileName, byte[] Content);

// A conferência pública de um PDF: quem emitiu e quando. Nada do paciente.
public sealed record PlanDocumentVerificationResponse(
    string Code,
    DateTime IssuedAtUtc,
    string NutritionistName,
    int CrnRegion,
    string CrnNumber,
    string Sha256);

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
    IPlanDocumentRepository documentRepository,
    IUnitOfWork unitOfWork,
    INutritionPlanPdfRenderer renderer,
    FrontendSettings frontendSettings,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<PlanPdfUseCase> logger)
{
    public async Task<PlanPdfFile?> RenderForPatientAsync(Guid patientId, Guid planId, CancellationToken cancellationToken = default)
    {
        var detail = await planQueries.GetByPatientAsync(patientId, planId, cancellationToken);

        return detail is null ? null : await IssueAsync(detail, cancellationToken);
    }

    public async Task<PlanPdfFile?> RenderForNutritionistAsync(Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default)
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

    public async Task<PlanDocumentVerificationResponse?> VerifyAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(code);
        if (normalized.Length != PlanDocument.CodeLength)
        {
            return null;
        }

        var document = await documentRepository.GetByCodeAsync(normalized, cancellationToken);

        return document is null
            ? null
            : new PlanDocumentVerificationResponse(
                FormatCode(document.Code), document.IssuedAtUtc, document.NutritionistName, document.CrnRegion, document.CrnNumber, document.Sha256);
    }

    // Sem 0, O, 1 e I: o código é lido e digitado por pessoas.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    // Cada PDF gerado é um documento: ganha um código verificador, e o registro da emissão fica guardado para a conferência pública.
    private async Task<PlanPdfFile> IssueAsync(NutritionPlanDetailResponse detail, CancellationToken cancellationToken)
    {
        var nutritionist = await userRepository.GetByIdAsync(detail.Plan.NutritionistId, cancellationToken);
        var profile = nutritionist?.NutritionistProfile
            ?? throw new InvalidOperationException($"O plano {detail.Plan.Id} não tem um nutricionista com CRN.");

        var code = RandomNumberGenerator.GetString(CodeAlphabet, PlanDocument.CodeLength);
        var issuedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var stamp = new PlanPdfStamp(
            FormatCode(code),
            BrazilianCalendar.ToBrasilia(issuedAtUtc),
            profile.CrnRegion,
            profile.CrnNumber,
            $"{frontendSettings.BaseUrl.TrimEnd('/')}/verificar-documento/{code}");

        var content = renderer.Render(detail, stamp);
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(content));

        documentRepository.Add(new PlanDocument(
            code, detail.Plan.Id, issuedAtUtc, detail.Plan.NutritionistName, profile.CrnRegion, profile.CrnNumber, sha256));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlanPdfFile($"nogvita-plano-{detail.Plan.StartDate:yyyy-MM-dd}.pdf", content);
    }

    // ABCD-EFGH-JKLM: mais fácil de ler e de digitar. A consulta aceita com ou sem os traços.
    public static string FormatCode(string code) => string.Join('-', code.Chunk(4).Select(part => new string(part)));

    public static string NormalizeCode(string code) => code.Replace("-", string.Empty).Trim().ToUpperInvariant();
}
