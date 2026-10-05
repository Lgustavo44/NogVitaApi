namespace NogVita.Application.Admin;

public sealed record AdminUserDetail(
    Guid Id,
    string Name,
    string Email,
    string Cpf,
    bool IsActive,
    bool IsAdmin,
    AdminPatientInfo? Patient,
    AdminNutritionistInfo? Nutritionist,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminPatientInfo(DateOnly BirthDate, string BiologicalSex, int HeightInCm, string Goal);

public sealed record AdminNutritionistInfo(int CrnRegion, string CrnNumber, bool IsActive);