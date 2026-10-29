using NogVita.Domain.Auth;

namespace NogVita.Application.Abstractions;

public interface IEmailConfirmationTokenRepository : ISingleUseTokenRepository<EmailConfirmationToken>;
