using NogVita.Domain.Common;

namespace NogVita.Domain.ProgressPhotos;

public enum ProgressPhotoAngle
{
    Front,
    Side,
    Back
}

public class ProgressPhoto : Entity
{
    public const int StoragePathMaxLength = 200;
    public const int MaxDaysInPast = 365;     // D-V2-15
    public const int MaxPerPatient = 90;      // D-V2-14

    public Guid PatientId { get; private set; }
    public DateOnly TakenOn { get; private set; }
    public ProgressPhotoAngle Angle { get; private set; }
    public string StoragePath { get; private set; } = null!;

    private ProgressPhoto() { } // EF Core

    public ProgressPhoto(Guid patientId, DateOnly takenOn, ProgressPhotoAngle angle, string storagePath, DateOnly today)
    {
        if (takenOn > today)
        {
            throw new DomainException("A data da foto não pode estar no futuro.");
        }

        if (takenOn < today.AddDays(-MaxDaysInPast))
        {
            throw new DomainException($"A foto pode ser de até {MaxDaysInPast} dias atrás.");
        }

        if (!Enum.IsDefined(angle))
        {
            throw new DomainException("Ângulo da foto inválido.");
        }

        PatientId = patientId;
        TakenOn = takenOn;
        Angle = angle;
        StoragePath = ValidPath(storagePath);
    }

    // D-V2-05: reenviar o mesmo ângulo na mesma data troca o arquivo. Devolve o caminho anterior,
    // que só é apagado depois de o banco confirmar (o mesmo cuidado da foto de perfil).
    public string ReplaceFile(string storagePath)
    {
        var previousPath = StoragePath;
        StoragePath = ValidPath(storagePath);
        return previousPath;
    }

    // O caminho é gerado pela API (D-V2-10): um caminho inválido aqui é bug, e não erro do usuário.
    private static string ValidPath(string storagePath) =>
        string.IsNullOrWhiteSpace(storagePath) || storagePath.Length > StoragePathMaxLength
            ? throw new ArgumentException("Caminho da foto inválido.", nameof(storagePath))
            : storagePath;
}
