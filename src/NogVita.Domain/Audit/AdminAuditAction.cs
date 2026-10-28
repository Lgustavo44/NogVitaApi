namespace NogVita.Domain.Audit;

// Todas as ações do admin que mudam dados (D173).
public enum AdminAuditAction
{
    UserActivated,
    UserDeactivated,
    NutritionistPreRegistered,
    NutritionistInvitationResent,
    NutritionistPreRegistrationCancelled,
    FoodActivated,
    FoodDeactivated
}
