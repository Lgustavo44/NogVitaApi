using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Application.Nutritionists;

public static class InvitationEmail
{
    public static EmailMessage Create(string to, string name, string link, bool isNewUser)
    {
        var heading = isNewUser ? "Boas-vindas ao NogVita!" : "Você tem um novo convite";

        var action = isNewUser
            ? "Para ativar sua conta de nutricionista, defina sua senha no link abaixo."
            : "Você foi convidado(a) a atuar como nutricionista no NogVita. Para aceitar, acesse o link abaixo com sua conta.";

        var buttonText = isNewUser ? "Definir minha senha" : "Aceitar convite";
        const string notice = "Este convite é válido por 72 horas e só pode ser usado uma vez. Se você não esperava este e-mail, ignore-o.";

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

        return new EmailMessage(to, "Seu convite para o NogVita", html, text);
    }
}
