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

    /// <summary>Apaga o perfil de nutricionista retirado por <see cref="User.RemovePendingNutritionistProfile"/>.</summary>
    void RemoveNutritionistProfile(NutritionistProfile profile);

    /// <summary>Apaga a conta e os tokens dela. Só para contas sem uso, como um pré-cadastro nunca aceito.</summary>
    Task RemoveAsync(User user, CancellationToken cancellationToken = default);
}
