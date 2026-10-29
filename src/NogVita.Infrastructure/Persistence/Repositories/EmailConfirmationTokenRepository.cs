using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;

namespace NogVita.Infrastructure.Persistence.Repositories;

public sealed class EmailConfirmationTokenRepository(NogVitaDbContext context)
    : SingleUseTokenRepository<EmailConfirmationToken>(context, revokeExpired: true), IEmailConfirmationTokenRepository;
