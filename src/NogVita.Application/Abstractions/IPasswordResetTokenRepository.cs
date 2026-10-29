using NogVita.Domain.Auth;

namespace NogVita.Application.Abstractions;

public interface IPasswordResetTokenRepository : ISingleUseTokenRepository<PasswordResetToken>;
