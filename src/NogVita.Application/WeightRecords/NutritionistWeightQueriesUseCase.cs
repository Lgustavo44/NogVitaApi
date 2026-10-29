using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;

namespace NogVita.Application.WeightRecords;

public sealed class NutritionistWeightQueriesUseCase(
    CareAccessService careAccessService,
    IWeightRecordQueries weightQueries)
{
    public async Task<PagedResponse<WeightRecordResponse>?> ListForPatientAsync(
        Guid nutritionistId, Guid patientId, WeightHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var followsPatient = await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, patientId, cancellationToken);

        return followsPatient
            ? await weightQueries.ListByPatientAsync(patientId, request, cancellationToken)
            : null;
    }
}
