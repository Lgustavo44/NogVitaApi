using NogVita.Domain.Users;

namespace NogVita.Application.Patients;

public sealed record PatientProfileResponse(
    Guid UserId,
    string Name,
    string Email,
    string Cpf,
    DateOnly BirthDate,
    int Age,
    BiologicalSex BiologicalSex,
    int HeightInCm,
    Goal Goal)
{
    public static PatientProfileResponse From(User user, PatientProfile profile, DateOnly today) =>
        new(
            user.Id,
            user.Name,
            user.Email,
            user.Cpf.Formatted,
            profile.BirthDate,
            profile.GetAgeAt(today),
            profile.BiologicalSex,
            profile.HeightInCm,
            profile.Goal);
}