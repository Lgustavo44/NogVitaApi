namespace NogVita.Application.Nutritionists;

public sealed record PreRegisterNutritionistRequest(
    string Name,
    string Email,
    string Cpf,
    int CrnRegion,
    string CrnNumber);