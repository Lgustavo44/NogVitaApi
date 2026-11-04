using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;

namespace NogVita.Application.Anamneses;

public sealed class NutritionistAnamnesisQueriesUseCase(
    CareAccessService careAccessService,
    IAnamnesisQueries anamnesisQueries)
{
    // D-V2-06: sem o vínculo, o nutricionista perde o acesso às anamneses (o paciente continua lendo as dele).
    public async Task<PagedResponse<AnamnesisSummaryResponse>?> ListForPatientAsync(
        Guid nutritionistId, Guid patientId, AnamnesisListRequest request, CancellationToken cancellationToken = default) =>
        await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, patientId, cancellationToken)
            ? await anamnesisQueries.ListByNutritionistAndPatientAsync(nutritionistId, patientId, request, cancellationToken)
            : null;

    public async Task<AnamnesisResponse?> GetForNutritionistAsync(
        Guid nutritionistId, Guid anamnesisId, CancellationToken cancellationToken = default)
    {
        var anamnesis = await anamnesisQueries.GetByNutritionistAsync(nutritionistId, anamnesisId, cancellationToken);

        if (anamnesis is null)
        {
            return null;
        }

        return await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, anamnesis.PatientId, cancellationToken)
            ? anamnesis
            : null;
    }
}
