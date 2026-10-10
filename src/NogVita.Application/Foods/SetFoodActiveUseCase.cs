using NogVita.Application.Abstractions;

namespace NogVita.Application.Foods;

public sealed class SetFoodActiveUseCase(IFoodRepository foodRepository, IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(Guid foodId, bool active, CancellationToken cancellationToken = default)
    {
        var food = await foodRepository.GetByIdAsync(foodId, cancellationToken);

        if (food is null)
        {
            return false;
        }

        if (active)
        {
            food.Activate();
        }
        else
        {
            food.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}