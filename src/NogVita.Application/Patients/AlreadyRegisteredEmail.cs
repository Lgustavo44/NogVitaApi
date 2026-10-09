using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Application.Patients;

public static class AlreadyRegisteredEmail
{
    // matchedByCpf: o pedido usou o CPF desta conta com outro e-mail.
    public static EmailMessage Create(string to, string name, bool matchedByCpf = false)
    {
        const string heading = "Você já tem uma conta";

        var reason = matchedByCpf
            ? "Recebemos um pedido de cadastro com o seu CPF e outro endereço de e-mail, mas o seu CPF já está vinculado a uma conta no NogVita."
            : "Recebemos um pedido de cadastro com este e-mail, mas você já tem uma conta no NogVita.";

        var action = matchedByCpf
            ? "Se foi você, basta fazer login com este e-mail e a sua senha."
            : "Se foi você, basta fazer login com o seu e-mail e a sua senha.";

        const string notice = "Se não foi você, pode ignorar este e-mail: nenhuma conta nova foi criada e nenhuma alteração foi feita na sua conta.";

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
