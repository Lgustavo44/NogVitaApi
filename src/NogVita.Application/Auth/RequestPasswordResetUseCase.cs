using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;

namespace NogVita.Application.Auth;

public sealed record RequestPasswordResetRequest(string Email);

public sealed class RequestPasswordResetRequestValidator : AbstractValidator<RequestPasswordResetRequest>
{
    public RequestPasswordResetRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(254);
    }
}

public sealed class RequestPasswordResetUseCase(
    IUserRepository userRepository,
    IPasswordResetTokenRepository tokenRepository,
    ISecureTokenService secureTokenService,
    PasswordResetNotificationService notificationService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task ExecuteAsync(RequestPasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Em todos os casos sem envio, a resposta é a mesma: não revela se o e-mail tem conta (D130).
        if (user is null)
        {
            return;
        }

        // Sem senha: nutricionista pré-cadastrado, que entra pelo convite (RN10).
        if (user.PasswordHash is null)
        {
            return;
        }

        // Bloqueado pelo admin (D163), com ou sem e-mail confirmado: a redefinição não pode reativar a conta.
        if (user.IsBlockedByAdmin)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Só o link mais recente vale.
        await tokenRepository.RevokePendingForUserAsync(user.Id, now, cancellationToken);

        var token = secureTokenService.GenerateToken();
        tokenRepository.Add(new PasswordResetToken(user.Id, secureTokenService.Hash(token), now.Add(TokenLifetime), now));

        // Salva antes de enviar: um link enviado precisa existir no banco. O e-mail leva o token, e o banco só o hash.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notificationService.TrySendResetLinkAsync(user, token, cancellationToken);
    }
}
