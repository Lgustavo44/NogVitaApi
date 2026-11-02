using NogVita.Domain.Users;

namespace NogVita.Application.Nutritionists;

public sealed record NutritionistProfileResponse(
    Guid UserId,
    string Name,
    string Email,
    int CrnRegion,
    string CrnNumber,
    string? Bio,
    string? PhotoUrl)
{
    public static NutritionistProfileResponse From(User user, string? photoUrl)
    {
        var profile = user.NutritionistProfile!;

        return new NutritionistProfileResponse(
            user.Id,
            user.Name,
            user.Email,
            profile.CrnRegion,
            profile.CrnNumber,
            profile.Bio,
            photoUrl);
    }
}
