using NogVita.Domain.Common;

namespace NogVita.Domain.Users
{
    public class PatientProfile : Entity
    {
        public Guid UserId { get; private set; }
        public DateOnly BirthDate { get; private set; }
        public BiologicalSex BiologicalSex { get; private set; }
        public int HeightInCm { get; private set; }
        public Goal Goal { get; private set; }

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

        private static void Validate(DateOnly birthDate, BiologicalSex biologicalSex, int heightInCm, Goal goal)
        {
            if (birthDate > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new DomainException("Data de nascimento não pode ser no futuro.");
            if (heightInCm <= 0)
                throw new DomainException("Altura deve ser maior que zero.");
            if (!Enum.IsDefined(typeof(BiologicalSex), biologicalSex))
                throw new DomainException("Sexo biológico inválido.");
            if (!Enum.IsDefined(typeof(Goal), goal))
                throw new DomainException("Objetivo inválido.");
        }
    }
}
