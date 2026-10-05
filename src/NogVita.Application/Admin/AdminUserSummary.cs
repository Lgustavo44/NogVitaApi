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