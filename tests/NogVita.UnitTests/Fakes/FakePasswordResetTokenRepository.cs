using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;

namespace NogVita.UnitTests.Fakes;

public sealed class FakePasswordResetTokenRepository : IPasswordResetTokenRepository
{
    public List<PasswordResetToken> Tokens { get; } = [];

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        foreach (var token in Tokens.Where(t => t.UserId == userId && t.IsValid(nowUtc)))
        {
            token.Revoke(nowUtc);
        }

        return Task.CompletedTask;
    }

    public void Add(PasswordResetToken token) => Tokens.Add(token);
}