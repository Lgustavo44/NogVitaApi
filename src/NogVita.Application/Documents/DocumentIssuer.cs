using System.Security.Cryptography;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Domain.Documents;

namespace NogVita.Application.Documents;

public sealed record PdfFile(string FileName, byte[] Content);

// O que identifica o documento (Res. CFN 760/2023, art. 10): o CRN, a data e a hora da emissão e o código verificador.
public sealed record DocumentStamp(
    string VerificationCode,
    DateTime IssuedAtBrasilia,
    int CrnRegion,
    string CrnNumber,
    string VerificationUrl);

// A conferência pública de um PDF: o que é, quem emitiu e quando. Nada do paciente.
public sealed record DocumentVerificationResponse(
    string Code,
    DocumentKind Kind,
    DateTime IssuedAtUtc,
    string NutritionistName,
    int CrnRegion,
    string CrnNumber,
    string Sha256);

// Cada PDF gerado (plano ou anamnese) é um documento: ganha um código verificador, e o registro da emissão
// fica guardado para a conferência pública.
public sealed class DocumentIssuer(
    IUserRepository userRepository,
    IIssuedDocumentRepository documentRepository,
    IUnitOfWork unitOfWork,
    FrontendSettings frontendSettings,
    TimeProvider timeProvider)
{
    // Sem 0, O, 1 e I: o código é lido e digitado por pessoas.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<byte[]> IssueAsync(
        DocumentKind kind,
        Guid sourceId,
        Guid nutritionistId,
        string nutritionistName,
        Func<DocumentStamp, byte[]> render,
        CancellationToken cancellationToken)
    {
        var nutritionist = await userRepository.GetByIdAsync(nutritionistId, cancellationToken);
        var profile = nutritionist?.NutritionistProfile
            ?? throw new InvalidOperationException($"O documento {kind} {sourceId} não tem um nutricionista com CRN.");

        var code = RandomNumberGenerator.GetString(CodeAlphabet, IssuedDocument.CodeLength);
        var issuedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var stamp = new DocumentStamp(
            FormatCode(code),
            BrazilianCalendar.ToBrasilia(issuedAtUtc),
            profile.CrnRegion,
            profile.CrnNumber,
            $"{frontendSettings.BaseUrl.TrimEnd('/')}/verificar-documento/{code}");

        var content = render(stamp);
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(content));

        documentRepository.Add(new IssuedDocument(
            code, kind, sourceId, issuedAtUtc, nutritionistName, profile.CrnRegion, profile.CrnNumber, sha256));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return content;
    }

    public async Task<DocumentVerificationResponse?> VerifyAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(code);
        if (normalized.Length != IssuedDocument.CodeLength)
        {
            return null;
        }

        var document = await documentRepository.GetByCodeAsync(normalized, cancellationToken);

        return document is null
            ? null
            : new DocumentVerificationResponse(
                FormatCode(document.Code), document.Kind, document.IssuedAtUtc, document.NutritionistName,
                document.CrnRegion, document.CrnNumber, document.Sha256);
    }

    // ABCD-EFGH-JKLM: mais fácil de ler e de digitar. A consulta aceita com ou sem os traços.
    public static string FormatCode(string code) => string.Join('-', code.Chunk(4).Select(part => new string(part)));

    public static string NormalizeCode(string code) => code.Replace("-", string.Empty).Trim().ToUpperInvariant();
}
