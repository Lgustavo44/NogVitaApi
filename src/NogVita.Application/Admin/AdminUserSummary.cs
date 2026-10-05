namespace NogVita.Application.Admin;

public sealed record AdminUserSummary(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    bool IsAdmin,
    bool IsPatient,
    bool IsNutritionist,
    DateTime CreatedAt);
public sealed record AdminPatientSummary(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    DateOnly BirthDate,
    string Goal,
    DateTime CreatedAt);

public sealed record AdminNutritionistSummary(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    int CrnRegion,
    string CrnNumber,
    bool IsProfileActive,
    DateTime CreatedAt);
