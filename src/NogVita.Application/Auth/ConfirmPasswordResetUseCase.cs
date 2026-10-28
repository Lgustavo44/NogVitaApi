using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.Common.Validation;

namespace NogVita.Application.Auth;

public sealed record ConfirmPasswordResetRequest(string Token, string NewPassword);

public sealed class ConfirmPasswordResetRequestValidator : AbstractValidator<ConfirmPasswordResetRequest>
{
    public ConfirmPasswordResetRequestValidator()
    {
        RuleFor(r => r.Token).NotEmpty().MaximumLength(200);

        // A mesma política de senha do cadastro (NIST SP 800-63B).
        RuleFor(r => r.NewPassword).StrongPassword();
    }
}

public sealed class ConfirmPasswordResetUseCase(
    IPasswordResetTokenRepository tokenRepository,
    IUserRepository userRepository,
    ISecureTokenService secureTokenService,
    IPasswordHasher passwordHasher,
    IRefreshTokenRepository refreshTokenRepository,
    PasswordResetNotificationService notificationService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<bool> ExecuteAsync(ConfirmPasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var token = await tokenRepository.GetByTokenHashAsync(secureTokenService.Hash(request.Token), cancellationToken);

        if (token is null || !token.IsValid(now))
        {
            return false;
        }

        // O usuário vem do token salvo, nunca de um Id enviado pelo cliente.
        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);

        // Sem senha (convite pendente) ou bloqueado pelo admin (D163): a redefinição não pode ativar a conta.
        if (user is null || user.PasswordHash is null || user.IsBlockedByAdmin)
        {
            return false;
        }

        token.MarkAsUsed(now);
        user.ResetPassword(passwordHasher.Hash(request.NewPassword), now);

        // Quem trocou a senha por suspeita de invasão precisa derrubar as sessões abertas do invasor.
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, now, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notificationService.TrySendPasswordChangedAsync(user, cancellationToken);

        return true;
    }
}