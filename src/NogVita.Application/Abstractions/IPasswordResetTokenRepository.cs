using NogVita.Domain.Auth;

namespace NogVita.Application.Abstractions;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokePendingForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    void Add(PasswordResetToken token);
}