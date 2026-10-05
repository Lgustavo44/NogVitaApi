using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(NogVitaDbContext context) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(email);

        return context.Users
            .Include(u => u.PatientProfile)
            .Include(u => u.NutritionistProfile)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Users
            .Include(u => u.PatientProfile)
            .Include(u => u.NutritionistProfile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(email);

        return context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        return context.Users.AnyAsync(u => u.Cpf == cpf, cancellationToken);
    }

    public void Add(User user)
    {
        context.Users.Add(user);
    }
}