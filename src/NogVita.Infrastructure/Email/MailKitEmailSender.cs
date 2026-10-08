using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NogVita.Application.Abstractions;

namespace NogVita.Infrastructure.Email;

public sealed class MailKitEmailSender(EmailSettings settings) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        }.ToMessageBody();

        using var client = new SmtpClient();

        var security = settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(settings.Host, settings.Port, security, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
            await client.AuthenticateAsync(settings.Username, settings.Password!, cancellationToken);

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}