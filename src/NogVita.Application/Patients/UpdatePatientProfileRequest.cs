using NogVita.Domain.Users;

namespace NogVita.Application.Patients;

public sealed record UpdatePatientProfileRequest(
    DateOnly BirthDate,
    BiologicalSex BiologicalSex,
    int HeightInCm,
    Goal Goal);