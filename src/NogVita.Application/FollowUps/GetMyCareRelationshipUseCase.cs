using NogVita.Application.Abstractions;

namespace NogVita.Application.FollowUps;

public sealed record MyCareRelationshipResponse(
    Guid RelationshipId,
    Guid NutritionistId,
    string NutritionistName,
    int CrnRegion,
    string CrnNumber,
    string? Bio,
    DateTime StartedAtUtc);

public sealed class GetMyCareRelationshipUseCase(
    ICareRelationshipRepository relationshipRepository,
    IUserRepository userRepository)
{
    public async Task<MyCareRelationshipResponse?> ExecuteAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var relationship = await relationshipRepository.GetActiveForPatientAsync(patientId, cancellationToken);

        if (relationship is null)
        {
            return null;
        }

        var nutritionist = await userRepository.GetByIdAsync(relationship.NutritionistId, cancellationToken);

        if (nutritionist?.NutritionistProfile is null)
        {
            return null;
        }

        return new MyCareRelationshipResponse(
            relationship.Id,
            nutritionist.Id,
            nutritionist.Name,
            nutritionist.NutritionistProfile.CrnRegion,
            nutritionist.NutritionistProfile.CrnNumber,
            nutritionist.NutritionistProfile.Bio,
            relationship.StartedAtUtc);
    }
}