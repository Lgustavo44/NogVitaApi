using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class PasswordResetTokenRepository(NogVitaDbContext context)
    : SingleUseTokenRepository<PasswordResetToken>(context, revokeExpired: false), IPasswordResetTokenRepository;
