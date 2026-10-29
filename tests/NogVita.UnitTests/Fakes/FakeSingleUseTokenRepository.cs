using NogVita.Application.Abstractions;
using NogVita.Domain.Common;

namespace NogVita.UnitTests.Fakes;

// Fase 28: o fake comum dos três repositórios de tokens, com a mesma regra do repositório real
// (SingleUseTokenRepository): revoga os pendentes do usuário, e os expirados só quando revokeExpired.
public abstract class FakeSingleUseTokenRepository<TToken>(bool revokeExpired) : ISingleUseTokenRepository<TToken>
    where TToken : SingleUseToken
{
    public List<TToken> Tokens { get; } = [];

    public Task<TToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        foreach (var token in Tokens.Where(t => t.UserId == userId && (revokeExpired || !t.IsExpired(nowUtc))))
        {
            token.Revoke(nowUtc);
        }

        return Task.CompletedTask;
    }

    public void Add(TToken token) => Tokens.Add(token);
}
