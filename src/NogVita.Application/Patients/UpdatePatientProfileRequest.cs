using NogVita.Domain.Users;

namespace NogVita.Application.Patients;

// Os horários da rotina são opcionais: enviar null (ou omitir) apaga o valor salvo.
public sealed record UpdatePatientProfileRequest(
    DateOnly BirthDate,
    BiologicalSex BiologicalSex,
    int HeightInCm,
    Goal Goal,
    TimeOnly? WakeUpTime = null,
    TimeOnly? BedTime = null,
    TimeOnly? TrainingTime = null);
