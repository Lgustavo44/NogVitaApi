using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Application.Patients;

public static class AlreadyRegisteredEmail
{
    public static EmailMessage Create(string to, string name)
    {
        const string heading = "Você já tem uma conta";
        const string reason = "Recebemos um pedido de cadastro com este e-mail, mas você já tem uma conta no NogVita.";
        const string action = "Se foi você, basta fazer login com o seu e-mail e a sua senha.";
        const string notice = "Se não foi você, pode ignorar este e-mail: nenhuma alteração foi feita na sua conta.";

        var html = EmailLayout.Render(
            preheader: reason,
            contentHtml: string.Join(Environment.NewLine,
                EmailLayout.Heading(heading),
                EmailLayout.Paragraph($"Olá, {name}!"),
                EmailLayout.Paragraph(reason),
                EmailLayout.Paragraph(action),
                EmailLayout.Notice(notice)));

        var text = $"""
            NogVita

            {heading}

            Olá, {name}!

            {reason}

            {action}

            {notice}

            --
            NogVita · Acompanhamento nutricional
            Este é um e-mail automático. Por favor, não responda.
            """;

        return new EmailMessage(to, "Você já tem uma conta no NogVita", html, text);
    }
}
