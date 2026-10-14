using NogVita.Application.Abstractions;

namespace NogVita.Application.FollowUps;

public sealed class CareAccessService(
    IUserRepository userRepository,
    ICareRelationshipRepository relationshipRepository)
{
    public async Task<bool> NutritionistFollowsPatientAsync(
        Guid nutritionistId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var nutritionist = await userRepository.GetByIdAsync(nutritionistId, cancellationToken);

        if (nutritionist is null || !nutritionist.IsActive || nutritionist.NutritionistProfile is not { IsActive: true })
        {
            return false;
        }

        var relationship = await relationshipRepository.GetActiveForPatientAsync(patientId, cancellationToken);

        return relationship is not null && relationship.NutritionistId == nutritionistId;
    }
}