using System.Net.Http.Json;
using NogVita.Application.Abstractions;

namespace NogVita.Infrastructure.Email;

public sealed class BrevoApiEmailSender(HttpClient httpClient, EmailSettings settings) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            sender = new { name = settings.FromName, email = settings.FromAddress },
            to = new[] { new { email = message.To } },
            subject = message.Subject,
            htmlContent = message.HtmlBody,
            textContent = message.TextBody
        };

        using var response = await httpClient.PostAsJsonAsync("v3/smtp/email", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"A Brevo recusou o envio ({(int)response.StatusCode}): {body}");
        }
    }
}