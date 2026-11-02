using NogVita.Application.Abstractions;
using NogVita.Application.Photos;

namespace NogVita.Application.Nutritionists;

public sealed class UpdateMyNutritionistProfileUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork, IFileStorage fileStorage)
{
    public async Task<NutritionistProfileResponse?> ExecuteAsync(
        Guid userId,
        UpdateMyNutritionistProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive || user.NutritionistProfile is not { IsActive: true })
        {
            return null;
        }

        user.UpdateNutritionistProfile(request.Bio);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NutritionistProfileResponse.From(user, fileStorage.NutritionistPhotoUrl(user.NutritionistProfile.PhotoPath));
    }
}
