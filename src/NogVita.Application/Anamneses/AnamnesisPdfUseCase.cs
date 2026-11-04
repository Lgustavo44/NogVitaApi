using NogVita.Application.Documents;
using NogVita.Domain.Anamneses;
using NogVita.Domain.Documents;

namespace NogVita.Application.Anamneses;

public interface IAnamnesisPdfRenderer
{
    byte[] Render(AnamnesisResponse anamnesis, DocumentStamp stamp);
}

public enum AnamnesisPdfStatus
{
    Done,
    NotFound,
    NotSubmitted
}

public sealed record AnamnesisPdfResult(AnamnesisPdfStatus Status, PdfFile? File = null);

// A anamnese faz parte do prontuário (Res. CFN 594/2017, art. 4º): o PDF serve para imprimir, guardar e assinar.
// Só a respondida tem o que imprimir. O nutricionista só com o vínculo ativo (D-V2-30); o paciente sempre, a dele.
public sealed class AnamnesisPdfUseCase(
    IAnamnesisQueries anamnesisQueries,
    NutritionistAnamnesisQueriesUseCase nutritionistQueries,
    IAnamnesisPdfRenderer renderer,
    DocumentIssuer documentIssuer)
{
    public async Task<AnamnesisPdfResult> RenderForNutritionistAsync(Guid nutritionistId, Guid anamnesisId, CancellationToken cancellationToken = default) =>
        await IssueAsync(await nutritionistQueries.GetForNutritionistAsync(nutritionistId, anamnesisId, cancellationToken), cancellationToken);

    public async Task<AnamnesisPdfResult> RenderForPatientAsync(Guid patientId, Guid anamnesisId, CancellationToken cancellationToken = default) =>
        await IssueAsync(await anamnesisQueries.GetByPatientAsync(patientId, anamnesisId, cancellationToken), cancellationToken);

    private async Task<AnamnesisPdfResult> IssueAsync(AnamnesisResponse? anamnesis, CancellationToken cancellationToken)
    {
        if (anamnesis is null)
        {
            return new AnamnesisPdfResult(AnamnesisPdfStatus.NotFound);
        }

        if (anamnesis.Status != AnamnesisStatus.Submitted)
        {
            return new AnamnesisPdfResult(AnamnesisPdfStatus.NotSubmitted);
        }

        var content = await documentIssuer.IssueAsync(
            DocumentKind.Anamnesis, anamnesis.Id, anamnesis.NutritionistId, anamnesis.NutritionistName,
            stamp => renderer.Render(anamnesis, stamp), cancellationToken);

        return new AnamnesisPdfResult(
            AnamnesisPdfStatus.Done, new PdfFile($"nogvita-anamnese-{anamnesis.SubmittedAt:yyyy-MM-dd}.pdf", content));
    }
}
