using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Application.FollowUps;

public static class CareRelationshipEndedEmail
{
    public static EmailMessage Create(string to, string recipientName, string endedByName)
    {
        const string heading = "Acompanhamento encerrado";
        var message = $"O acompanhamento nutricional com {endedByName} foi encerrado no NogVita.";
        const string help = "Se tiver dúvidas, acesse a sua conta no NogVita.";

        var html = EmailLayout.Render(
            preheader: message,
            contentHtml: string.Join(Environment.NewLine,
                EmailLayout.Heading(heading),
                EmailLayout.Paragraph($"Olá, {recipientName}!"),
                EmailLayout.Paragraph(message),
                EmailLayout.Paragraph(help)));

        var text = $"""
            NogVita

            {heading}

            Olá, {recipientName}!

            {message}

            {help}

            --
            NogVita · Acompanhamento nutricional
            Este é um e-mail automático. Por favor, não responda.
            """;

        return new EmailMessage(to, "Acompanhamento encerrado no NogVita", html, text);
    }
}
