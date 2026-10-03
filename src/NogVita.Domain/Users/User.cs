using NogVita.Domain.Common;

namespace NogVita.Domain.Users
{
    public class User : Entity
    {
        public string Name { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string Cpf { get; private set; } = null!;
        public string? PasswordHash { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsAdmin { get; private set; }
        public PatientProfile? PatientProfile { get; private set; }

        private User() { } // usado pelo EF Core

        public User(string name, string email, string cpf)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("O nome é obrigatório.");
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("O email é obrigatório.");
            if (string.IsNullOrWhiteSpace(cpf))
                throw new DomainException("O CPF é obrigatório.");
            Name = name.Trim();
            Email = email.Trim().ToLowerInvariant();
            Cpf = cpf.Trim();
        }

        public PatientProfile CreatePatientProfile(DateOnly birthDate, BiologicalSex biologicalSex, int heightInCm, Goal goal)
        {
            if (PatientProfile is not null)
                throw new DomainException("O usuário já possui perfil de paciente.");

            PatientProfile = new PatientProfile(Id, birthDate, biologicalSex, heightInCm, goal);
            return PatientProfile;
        }

        public void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("A senha é obrigatória.");
            PasswordHash = passwordHash;
        }

        public void Activate()
        {
            if (PasswordHash is null)
                throw new DomainException("Não é possível ativar um usuário sem senha definida.");

            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
