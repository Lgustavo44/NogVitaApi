using System.Net;

namespace NogVita.Application.Common;

// Layout HTML comum dos e-mails. Usa tabelas e estilos inline porque é o que os clientes de e-mail renderizam de forma consistente.
public static class EmailLayout
{
    // Content-ID da logo, anexada inline pelo IEmailSender (SVG não é exibido pelo Gmail nem pelo Outlook).
    public const string LogoContentId = "nogvita-logo";

    private const string Orange = "#F28C28";
    private const string Pulp = "#FFF4E3";
    private const string DarkGreen = "#1D2A24";
    private const string Muted = "#5B6B63";
    private const string Background = "#F4F6F5";
    private const string FontStack = "Arial, Helvetica, sans-serif";

    // contentHtml já deve vir com os dados do usuário codificados.
    public static string Render(string preheader, string contentHtml)
    {
        var safePreheader = WebUtility.HtmlEncode(preheader);

        return $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="color-scheme" content="light">
              <title>NogVita</title>
            </head>
            <body style="margin:0; padding:0; background-color:{{Background}};">
              <div style="display:none; max-height:0; overflow:hidden; opacity:0;">{{safePreheader}}</div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:{{Background}};">
                <tr>
                  <td align="center" style="padding:32px 16px;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px; background-color:#FFFFFF; border-radius:12px; overflow:hidden;">
                      <tr>
                        <td style="height:6px; background-color:{{Orange}}; font-size:0; line-height:0;">&nbsp;</td>
                      </tr>
                      <tr>
                        <td align="center" style="padding:32px 32px 8px;">
                          <img src="cid:{{LogoContentId}}" width="180" alt="NogVita" style="display:block; width:180px; max-width:100%; height:auto; border:0; font-family:{{FontStack}}; font-size:28px; font-weight:bold; color:{{DarkGreen}};">
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:16px 40px 40px; font-family:{{FontStack}}; font-size:16px; line-height:1.6; color:{{DarkGreen}};">
                          {{contentHtml}}
                        </td>
                      </tr>
                    </table>
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;">
                      <tr>
                        <td align="center" style="padding:24px 16px; font-family:{{FontStack}}; font-size:12px; line-height:1.5; color:{{Muted}};">
                          NogVita · Acompanhamento nutricional<br>
                          Este é um e-mail automático. Por favor, não responda.
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    public static string Heading(string text) =>
        $"""<h1 style="margin:0 0 16px; font-family:{FontStack}; font-size:22px; line-height:1.3; color:{DarkGreen};">{WebUtility.HtmlEncode(text)}</h1>""";

    public static string Paragraph(string text) =>
        $"""<p style="margin:0 0 16px;">{WebUtility.HtmlEncode(text)}</p>""";

    public static string Button(string text, string href)
    {
        var safeHref = WebUtility.HtmlEncode(href);

        return $"""
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:8px 0 24px;">
              <tr>
                <td align="center" style="border-radius:8px; background-color:{DarkGreen};">
                  <a href="{safeHref}" target="_blank" style="display:inline-block; padding:14px 28px; font-family:{FontStack}; font-size:16px; font-weight:bold; color:#FFFFFF; text-decoration:none; border-radius:8px;">{WebUtility.HtmlEncode(text)}</a>
                </td>
              </tr>
            </table>
            """;
    }

    // Link em texto para quando o botão não funciona no cliente de e-mail.
    public static string FallbackLink(string href)
    {
        var safeHref = WebUtility.HtmlEncode(href);

        return $"""
            <p style="margin:0 0 4px; font-size:13px; color:{Muted};">Se o botão não funcionar, copie e cole este endereço no navegador:</p>
            <p style="margin:0 0 24px; font-size:13px; word-break:break-all;"><a href="{safeHref}" target="_blank" style="color:{DarkGreen};">{safeHref}</a></p>
            """;
    }

    public static string Notice(string text) =>
        $"""
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
          <tr>
            <td style="padding:12px 16px; background-color:{Pulp}; border-left:4px solid {Orange}; border-radius:4px; font-family:{FontStack}; font-size:14px; line-height:1.5; color:{DarkGreen};">{WebUtility.HtmlEncode(text)}</td>
          </tr>
        </table>
        """;
}
