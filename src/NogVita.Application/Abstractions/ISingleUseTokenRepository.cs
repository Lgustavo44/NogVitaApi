using NogVita.Domain.Common;

namespace NogVita.Application.Abstractions;

// Fase 28: os três tokens de uso único (confirmação de e-mail, redefinição de senha e convite) são guardados
// do mesmo jeito. Os repositórios deles têm este contrato em comum.
public interface ISingleUseTokenRepository<TToken> where TToken : SingleUseToken
{
    Task<TToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    // Revoga os tokens ainda pendentes do usuário: só o link mais recente vale.
    Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);

    void Add(TToken token);
}
