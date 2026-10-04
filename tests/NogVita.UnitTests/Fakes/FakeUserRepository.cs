using NogVita.Application.Abstractions;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeUserRepository(User? user) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(user?.Id == id ? user : null);
    }
}