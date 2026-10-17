using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Application.NutritionPlans;

public static class PlanPdfEmail
{
    public static EmailMessage Create(string to, string name, string planName, string fileName, byte[] pdf)
    {
        const string heading = "Seu plano alimentar";
        var message = $"Segue em anexo o seu plano alimentar \"{planName}\", em PDF, como você pediu no NogVita.";
        const string notice = "Este e-mail foi enviado porque você pediu. Se não foi você, recomendamos trocar a sua senha.";

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

        return new EmailMessage(
            to,
            $"Seu plano alimentar: {planName}",
            html,
            text,
            [new EmailAttachment(fileName, "application/pdf", pdf)]);
    }
}
