using System.Net;
using NogVita.Application.Abstractions;

namespace NogVita.Application.Nutritionists;

public static class InvitationEmail
{
    public static EmailMessage Create(string to, string name, string link, bool isNewUser)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeLink = WebUtility.HtmlEncode(link);

        var action = isNewUser
            ? "Para ativar sua conta de nutricionista, defina sua senha no link abaixo."
            : "Você foi convidado(a) a atuar como nutricionista no NogVita. Para aceitar, acesse o link abaixo com sua conta.";

        var html = $"""
            <p>Olá, {safeName}!</p>
            <p>{action}</p>
            <p><a href="{safeLink}">Aceitar convite</a></p>
            <p>Este convite é válido por 72 horas. Se você não esperava este e-mail, ignore-o.</p>
            """;

        var text = $"""
            Olá, {name}!

            {action}

            {link}

            Este convite é válido por 72 horas. Se você não esperava este e-mail, ignore-o.
            """;

        return new EmailMessage(to, "Seu convite para o NogVita", html, text);
    }
}