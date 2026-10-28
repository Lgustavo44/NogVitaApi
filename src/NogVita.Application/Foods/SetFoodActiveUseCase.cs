using NogVita.Application.Abstractions;
using NogVita.Application.Admin;
using NogVita.Domain.Audit;

namespace NogVita.Application.Foods;

public sealed class SetFoodActiveUseCase(IFoodRepository foodRepository, IUnitOfWork unitOfWork, AdminAuditLog auditLog)
{
    public async Task<bool> ExecuteAsync(Guid adminId, Guid foodId, bool active, CancellationToken cancellationToken = default)
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

        auditLog.Record(adminId, active ? AdminAuditAction.FoodActivated : AdminAuditAction.FoodDeactivated, food.Id);
        return true;
    }
}
