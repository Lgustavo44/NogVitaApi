using NogVita.Domain.Users;

namespace NogVita.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);
    Task<User?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCrnAsync(int crnRegion, string crnNumber, CancellationToken cancellationToken = default);
    void Add(User user);
}