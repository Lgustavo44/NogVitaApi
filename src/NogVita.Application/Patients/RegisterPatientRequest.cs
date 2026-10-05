using NogVita.Domain.Users;

namespace NogVita.Application.Patients;

public sealed record RegisterPatientRequest(
    string Name,
    string Email,
    string Cpf,
    string Password,
    DateOnly BirthDate,
    BiologicalSex BiologicalSex,
    int HeightInCm,
    Goal Goal);