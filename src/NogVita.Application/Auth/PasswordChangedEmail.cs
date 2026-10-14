using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Application.Auth;

public static class PasswordChangedEmail
{
    public static EmailMessage Create(string to, string name)
    {
        const string heading = "Sua senha foi alterada";
        const string message = "A senha da sua conta no NogVita acabou de ser alterada, e todas as sessões abertas foram encerradas.";
        const string notice = "Se não foi você, peça uma nova redefinição de senha imediatamente pelo \"Esqueci minha senha\".";

        var html = EmailLayout.Render(
            preheader: message,
            contentHtml: string.Join(Environment.NewLine,
                EmailLayout.Heading(heading),
                EmailLayout.Paragraph($"Olá, {name}!"),
                EmailLayout.Paragraph(message),
                EmailLayout.Notice(notice)));

        var text = $"""
            NogVita

            {heading}

            Olá, {name}!

            {message}

            {notice}

            --
            NogVita · Acompanhamento nutricional
            Este é um e-mail automático. Por favor, não responda.
            """;

        return new EmailMessage(to, "Sua senha do NogVita foi alterada", html, text);
    }
}
