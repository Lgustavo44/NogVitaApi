namespace NogVita.Application.Abstractions;

public enum PasswordCheck
{
    Failed,
    Success,

    /// <summary>A senha confere, mas o hash usa parâmetros antigos e deve ser gerado de novo.</summary>
    SuccessRehashNeeded
}

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Confere a senha com o hash. Com <paramref name="passwordHash"/> nulo (usuário inexistente ou sem senha),
    /// faz a mesma conta contra um hash falso e devolve <see cref="PasswordCheck.Failed"/>: o tempo de resposta
    /// não revela se o e-mail está cadastrado.
    /// </summary>
    PasswordCheck Verify(string? passwordHash, string password);
}
