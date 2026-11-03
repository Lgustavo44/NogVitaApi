using NogVita.Domain.Common;

namespace NogVita.Domain.Users;

public class PatientProfile : Entity
{
    // Idade mínima para usar o NogVita (Termos de Uso, seção 3).
    public const int MinimumAge = 18;

    public Guid UserId { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public BiologicalSex BiologicalSex { get; private set; }
    public int HeightInCm { get; private set; }
    public Goal Goal { get; private set; }
    public const int PhotoPathMaxLength = 200;
    public string? PhotoPath { get; private set; }
    public const int ConsentVersionMaxLength = 20;
    public DateTime? ProgressPhotosConsentAtUtc { get; private set; }
    public string? ProgressPhotosConsentVersion { get; private set; }

    // LGPD, arts. 8º e 11, I: o aceite dos dados de saúde (e da transferência internacional, art. 33, VIII), com a prova de quando e de qual texto.
    public DateTime? HealthDataConsentAtUtc { get; private set; }
    public string? HealthDataConsentVersion { get; private set; }

    // Rotina, opcional e no horário local do paciente. Sem regra de "janela": quem trabalha à noite acorda depois de dormir.
    public TimeOnly? WakeUpTime { get; private set; }
    public TimeOnly? BedTime { get; private set; }
    public TimeOnly? TrainingTime { get; private set; }

    private PatientProfile() { }

    internal PatientProfile(Guid userId, DateOnly birthDate, BiologicalSex biologicalSex, int heightInCm, Goal goal)
    {
        Validate(birthDate, biologicalSex, heightInCm, goal);
        UserId = userId;
        BirthDate = birthDate;
        BiologicalSex = biologicalSex;
        HeightInCm = heightInCm;
        Goal = goal;
    }

    internal void Update(DateOnly birthDate, BiologicalSex biologicalSex, int heightInCm, Goal goal)
    {
        Validate(birthDate, biologicalSex, heightInCm, goal);
        BirthDate = birthDate;
        BiologicalSex = biologicalSex;
        HeightInCm = heightInCm;
        Goal = goal;
    }

    internal void UpdateRoutine(TimeOnly? wakeUpTime, TimeOnly? bedTime, TimeOnly? trainingTime)
    {
        if (wakeUpTime is not null && wakeUpTime == bedTime)
        {
            throw new DomainException("A hora de acordar e a de dormir não podem ser iguais.");
        }

        WakeUpTime = wakeUpTime;
        BedTime = bedTime;
        TrainingTime = trainingTime;
    }

    private static void Validate(DateOnly birthDate, BiologicalSex biologicalSex, int heightInCm, Goal goal)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (birthDate > today)
        {
            throw new DomainException("Data de nascimento não pode ser no futuro.");
        }

        if (CalculateAge(birthDate, today) < MinimumAge)
        {
            throw new DomainException($"O paciente precisa ter pelo menos {MinimumAge} anos.");
        }

        if (heightInCm <= 0)
        {
            throw new DomainException("Altura deve ser maior que zero.");
        }

        if (!Enum.IsDefined(typeof(BiologicalSex), biologicalSex))
        {
            throw new DomainException("Sexo biológico inválido.");
        }

        if (!Enum.IsDefined(typeof(Goal), goal))
        {
            throw new DomainException("Objetivo inválido.");
        }
    }

    public int GetAgeAt(DateOnly date) => CalculateAge(BirthDate, date);

    public static int CalculateAge(DateOnly birthDate, DateOnly date)
    {
        var age = date.Year - birthDate.Year;
        if (date.Month < birthDate.Month || (date.Month == birthDate.Month && date.Day < birthDate.Day))
        {
            age--;
        }
        return age;
    }

    // Devolve o caminho anterior: o arquivo antigo só é apagado depois que o banco confirma a troca.
    internal string? ChangePhoto(string? photoPath)
    {
        // O caminho é gerado pela API (D-V2-10): um caminho inválido aqui é bug, e não erro do usuário.
        if (photoPath is not null && (string.IsNullOrWhiteSpace(photoPath) || photoPath.Length > PhotoPathMaxLength))
        {
            throw new ArgumentException("Caminho da foto inválido.", nameof(photoPath));
        }

        var previousPath = PhotoPath;
        PhotoPath = photoPath;
        return previousPath;
    }

    // D-V2-16: só vale o aceite da versão atual do termo. Um método, e não uma propriedade:
    // ele depende da versão atual, e nunca entra numa consulta do EF.
    public bool HasProgressPhotosConsent(string currentVersion) =>
        ProgressPhotosConsentAtUtc is not null && ProgressPhotosConsentVersion == currentVersion;

    internal void GrantProgressPhotosConsent(string version, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Length > ConsentVersionMaxLength)
        {
            throw new ArgumentException("Versão do termo inválida.", nameof(version));
        }

        ProgressPhotosConsentAtUtc = nowUtc;
        ProgressPhotosConsentVersion = version;
    }

    public bool HasHealthDataConsent(string currentVersion) =>
        HealthDataConsentAtUtc is not null && HealthDataConsentVersion == currentVersion;

    internal void GrantHealthDataConsent(string version, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Length > ConsentVersionMaxLength)
        {
            throw new ArgumentException("Versão do termo inválida.", nameof(version));
        }

        HealthDataConsentAtUtc = nowUtc;
        HealthDataConsentVersion = version;
    }

    internal void RevokeProgressPhotosConsent()
    {
        ProgressPhotosConsentAtUtc = null;
        ProgressPhotosConsentVersion = null;
    }
}
