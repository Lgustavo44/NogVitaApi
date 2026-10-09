using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;
using NogVita.Domain.Invitations;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeEmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    public List<EmailConfirmationToken> Tokens { get; } = [];

    public Task<EmailConfirmationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Tokens.FirstOrDefault(i => i.TokenHash == tokenHash));
    }

    public Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        foreach (var token in Tokens.Where(i => i.UserId == userId))
            token.Revoke(nowUtc);

        return Task.CompletedTask;
    }

    public void Add(EmailConfirmationToken token)
    {
        Tokens.Add(token);
    }
}