namespace NogVita.Application.Nutritionists;

public sealed record PreRegisterNutritionistResponse(Guid UserId, bool InvitationEmailSent);