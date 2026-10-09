using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Application.Patients;

public static class ConfirmationEmail
{
    public static EmailMessage Create(string to, string name, string link)
    {
        const string heading = "Confirme seu e-mail";
        const string action = "Para ativar sua conta no NogVita, confirme seu e-mail no link abaixo.";
        const string buttonText = "Confirmar e-mail";
        const string notice = "Este link é válido por 24 horas e só pode ser usado uma vez. Se você não criou uma conta no NogVita, ignore este e-mail.";

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

        return new EmailMessage(to, "Confirme seu e-mail no NogVita", html, text);
    }
}
