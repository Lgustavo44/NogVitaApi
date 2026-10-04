using NogVita.Domain.Common;

namespace NogVita.Domain.Users
{
    public class User : Entity
    {
        public string Name { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public Cpf Cpf { get; private set; } = null!;
        public string? PasswordHash { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsAdmin { get; private set; }
        public PatientProfile? PatientProfile { get; private set; }
        public NutritionistProfile? NutritionistProfile { get; private set; }

        private User() { } // usado pelo EF Core

        public User(string name, string email, Cpf cpf)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("O nome é obrigatório.");
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("O email é obrigatório.");
            ArgumentNullException.ThrowIfNull(cpf);
            Name = name.Trim();
            Email = NormalizeEmail(email);
            Cpf = cpf;
        }

        public PatientProfile CreatePatientProfile(DateOnly birthDate, BiologicalSex biologicalSex, int heightInCm, Goal goal)
        {
            if (PatientProfile is not null)
                throw new DomainException("O usuário já possui perfil de paciente.");

            PatientProfile = new PatientProfile(Id, birthDate, biologicalSex, heightInCm, goal);
            return PatientProfile;
        }

        public NutritionistProfile CreateNutritionistProfile(int crnRegion, string crnNumber)
        {
            if (NutritionistProfile is not null)
                throw new DomainException("O usuário já possui perfil de nutricionista.");
            NutritionistProfile = new NutritionistProfile(Id, crnRegion, crnNumber);
            return NutritionistProfile;
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

        public void GrantAdmin()
        {
            if (!IsActive)
                throw new DomainException("Somente usuários ativos podem ser administradores.");

            IsAdmin = true;
        }

        public static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        public IReadOnlyList<string> GetRoles()
        {
            var roles = new List<string>();

            if (IsAdmin)         
                roles.Add(Roles.Admin);
            if (PatientProfile is not null)
                roles.Add(Roles.Patient);
            if (NutritionistProfile is not null && NutritionistProfile.IsActive)
                roles.Add(Roles.Nutritionist); 

            return roles;
        }
    }
}
