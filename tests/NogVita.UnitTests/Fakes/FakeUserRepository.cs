using NogVita.Application.Abstractions;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public FakeUserRepository(User? user = null)
    {
        if (user is not null)
            Users.Add(user);
    }

    public Task<User?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Users.FirstOrDefault(u => u.Cpf == cpf));
    }

    public Task<bool> ExistsByCrnAsync(int crnRegion, string crnNumber, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = crnNumber.Trim();

        return Task.FromResult(Users.Any(u =>
            u.NutritionistProfile is not null &&
            u.NutritionistProfile.CrnRegion == crnRegion &&
            u.NutritionistProfile.CrnNumber == normalizedNumber));
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        return Task.FromResult(Users.FirstOrDefault(u => u.Email == normalizedEmail));
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        return Task.FromResult(Users.Any(u => u.Email == normalizedEmail));
    }

    public Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Users.Any(u => u.Cpf == cpf));
    }

    public void Add(User user)
    {
        Users.Add(user);
    }
}