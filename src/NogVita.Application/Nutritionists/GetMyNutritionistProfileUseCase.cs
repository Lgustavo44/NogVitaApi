using NogVita.Application.Abstractions;
using NogVita.Application.Photos;

namespace NogVita.Application.Nutritionists;

public sealed class GetMyNutritionistProfileUseCase(IUserRepository userRepository, IFileStorage fileStorage)
{
    public async Task<NutritionistProfileResponse?> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive || user.NutritionistProfile is not { IsActive: true })
        {
            return null;
        }

        return NutritionistProfileResponse.From(user, fileStorage.NutritionistPhotoUrl(user.NutritionistProfile.PhotoPath));
    }
}
