using NogVita.Domain.Common;

namespace NogVita.Domain.Users
{
    public class PatientProfile : Entity
    {
        // Idade mínima para usar o NogVita (Termos de Uso, seção 3).
        public const int MinimumAge = 18;

        public Guid UserId { get; private set; }
        public DateOnly BirthDate { get; private set; }
        public BiologicalSex BiologicalSex { get; private set; }
        public int HeightInCm { get; private set; }
        public Goal Goal { get; private set; }

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
                throw new DomainException("A hora de acordar e a de dormir não podem ser iguais.");

            WakeUpTime = wakeUpTime;
            BedTime = bedTime;
            TrainingTime = trainingTime;
        }

        private static void Validate(DateOnly birthDate, BiologicalSex biologicalSex, int heightInCm, Goal goal)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (birthDate > today)
                throw new DomainException("Data de nascimento não pode ser no futuro.");
            if (CalculateAge(birthDate, today) < MinimumAge)
                throw new DomainException($"O paciente precisa ter pelo menos {MinimumAge} anos.");
            if (heightInCm <= 0)
                throw new DomainException("Altura deve ser maior que zero.");
            if (!Enum.IsDefined(typeof(BiologicalSex), biologicalSex))
                throw new DomainException("Sexo biológico inválido.");
            if (!Enum.IsDefined(typeof(Goal), goal))
                throw new DomainException("Objetivo inválido.");
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
    }
}
