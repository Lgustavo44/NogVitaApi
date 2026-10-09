using NogVita.Application.Abstractions;

namespace NogVita.Application.Nutritionists;

public sealed class GetMyNutritionistProfileUseCase(IUserRepository userRepository)
{
    public async Task<NutritionistProfileResponse?> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive || user.NutritionistProfile is not { IsActive: true })
        {
            return null;
        }

        return NutritionistProfileResponse.From(user);
    }
}