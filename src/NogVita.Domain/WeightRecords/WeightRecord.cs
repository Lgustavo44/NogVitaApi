using NogVita.Domain.Common;

namespace NogVita.Domain.WeightRecords;

public class WeightRecord : Entity
{
    public const decimal MinWeightKg = 20m;
    public const decimal MaxWeightKg = 400m;
    public const int NotesMaxLength = 300;
    public const int MaxDaysInPast = 365;

    public Guid PatientId { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public DateOnly RecordedOn { get; private set; }
    public decimal WeightKg { get; private set; }
    public string? Notes { get; private set; }

    public bool WasRecordedByPatient => RecordedByUserId == PatientId;

    private WeightRecord() { } // EF Core

    public WeightRecord(
        Guid patientId,
        Guid recordedByUserId,
        DateOnly recordedOn,
        decimal weightKg,
        string? notes,
        DateOnly today)
    {
        ValidateDate(recordedOn, today);

        PatientId = patientId;
        RecordedByUserId = recordedByUserId;
        RecordedOn = recordedOn;
        SetValues(weightKg, notes);
    }

    public bool WasRecordedBy(Guid userId) => RecordedByUserId == userId;

    public void Update(Guid userId, decimal weightKg, string? notes)
    {
        if (!WasRecordedBy(userId))
        {
            throw new DomainException("Só quem fez o registro pode alterá-lo.");
        }

        SetValues(weightKg, notes);
    }

    private static void ValidateDate(DateOnly recordedOn, DateOnly today)
    {
        if (recordedOn < today.AddDays(-MaxDaysInPast))
        {
            throw new DomainException($"O peso pode ser registrado com até {MaxDaysInPast} dias de antecedência.");
        }
        if (recordedOn > today)
        {
            throw new DomainException("O peso não pode ser registrado com uma data futura.");
        }
    }

    private void SetValues(decimal weightKg, string? notes)
    {
        if (weightKg < MinWeightKg || weightKg > MaxWeightKg)
        {
            throw new DomainException($"O peso precisa estar entre {MinWeightKg} e {MaxWeightKg} kg.");
        }

        var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (normalizedNotes is { Length: > NotesMaxLength })
        {
            throw new DomainException($"As observações não podem ter mais que {NotesMaxLength} caracteres.");
        }

        // Só grava depois de validar tudo: um Update recusado não deixa o registro pela metade.
        // Duas casas decimais (78,456 kg vira 78,46 kg): mais que isso a balança não mede.
        WeightKg = Math.Round(weightKg, 2, MidpointRounding.AwayFromZero);
        Notes = normalizedNotes;
    }
}
