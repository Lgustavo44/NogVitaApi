using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Common.Validation;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Seed;

public sealed class AdminSeeder(
    NogVitaDbContext context,
    IPasswordHasher passwordHasher,
    ILogger<AdminSeeder> logger)
{
    private const int MinPasswordLength = PasswordRules.MinLength;

    public async Task SeedAsync(AdminSeedSettings? settings, CancellationToken cancellationToken = default)
    {
        if (settings is null)
        {
            logger.LogWarning("Seção {Section} não configurada. Nenhum administrador será criado.", AdminSeedSettings.SectionName);
            return;
        }

        if (await context.Users.AnyAsync(u => u.IsAdmin, cancellationToken))
            return;

        if (settings.Password.Length < MinPasswordLength)
        {
            throw new InvalidOperationException($"A senha deve ter pelo menos {MinPasswordLength} caracteres.");
        }

        var admin = new User(settings.Name, settings.Email, Cpf.Create(settings.Cpf));
        admin.SetPasswordHash(passwordHasher.Hash(settings.Password));
        admin.ConfirmEmail(DateTime.UtcNow);
        admin.GrantAdmin();

        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Administrador inicial criado com Id {UserId}.", admin.Id);
    }
}