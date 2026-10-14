using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Application.Auth;

public static class PasswordResetEmail
{
    public static EmailMessage Create(string to, string name, string link)
    {
        const string heading = "Redefinição de senha";
        const string action = "Recebemos um pedido para redefinir a senha da sua conta no NogVita. Para criar uma nova senha, use o link abaixo.";
        const string buttonText = "Criar uma nova senha";
        const string notice = "Este link é válido por 1 hora e só pode ser usado uma vez. Se você não fez esse pedido, ignore este e-mail: a sua senha continua a mesma.";

        var html = EmailLayout.Render(
            preheader: action,
            contentHtml: string.Join(Environment.NewLine,
                EmailLayout.Heading(heading),
                EmailLayout.Paragraph($"Olá, {name}!"),
                EmailLayout.Paragraph(action),
                EmailLayout.Button(buttonText, link),
                EmailLayout.FallbackLink(link),
                EmailLayout.Notice(notice)));

        var text = $"""
            NogVita

            {heading}

            Olá, {name}!

            {action}

            {buttonText}: {link}

            {notice}

            --
            NogVita · Acompanhamento nutricional
            Este é um e-mail automático. Por favor, não responda.
            """;

        return new EmailMessage(to, "Redefinição de senha do NogVita", html, text);
    }
}
