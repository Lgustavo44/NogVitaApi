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
        public DateTime? EmailConfirmedAtUtc { get; private set; }
        public bool IsEmailConfirmed => EmailConfirmedAtUtc is not null;
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

        public void UpdatePatientProfile(DateOnly birthDate, BiologicalSex biologicalSex, int heightInCm, Goal goal)
        {
            if (PatientProfile is null)
                throw new DomainException("O usuário não possui perfil de paciente.");

            PatientProfile.Update(birthDate, biologicalSex, heightInCm, goal);
        }

        public void UpdatePatientRoutine(TimeOnly? wakeUpTime, TimeOnly? bedTime, TimeOnly? trainingTime)
        {
            if (PatientProfile is null)
                throw new DomainException("O usuário não possui perfil de paciente.");

            PatientProfile.UpdateRoutine(wakeUpTime, bedTime, trainingTime);
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

        public void ConfirmEmail(DateTime nowUtc)
        {
            if (IsEmailConfirmed) return;
            EmailConfirmedAtUtc = nowUtc;
            Activate();
        }

        public void ResetPassword(string passwordHash, DateTime nowUtc)
        {
            SetPasswordHash(passwordHash);

            if (!IsEmailConfirmed)
            {
                ConfirmEmail(nowUtc);
            }
        }

        // Cancela um pré-cadastro cujo convite nunca foi aceito, liberando o CRN.
        public NutritionistProfile RemovePendingNutritionistProfile()
        {
            if (NutritionistProfile is null)
                throw new DomainException("O usuário não possui perfil de nutricionista.");
            if (NutritionistProfile.IsActive)
                throw new DomainException("O nutricionista já aceitou o convite.");

            var profile = NutritionistProfile;
            NutritionistProfile = null;
            return profile;
        }

        // Conta criada só pelo pré-cadastro: nunca definiu senha nem tem perfil de paciente.
        public bool IsOnlyPendingNutritionist =>
            PasswordHash is null && PatientProfile is null && NutritionistProfile is { IsActive: false };

        public void UpdateNutritionistProfile(string? bio)
        {
            if (NutritionistProfile is null)
            {
                throw new DomainException("O usuário não possui perfil de nutricionista.");
            }

            NutritionistProfile.UpdateBio(bio);
        }
    }
}
