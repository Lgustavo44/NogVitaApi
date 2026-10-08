using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;

namespace NogVita.Infrastructure.Email;

public sealed class MailKitEmailSender(EmailSettings settings) : IEmailSender
{
    private static readonly Lazy<byte[]> LogoPng = new(LoadLogo);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        };

        if (message.HtmlBody.Contains($"cid:{EmailLayout.LogoContentId}", StringComparison.Ordinal))
        {
            var logo = builder.LinkedResources.Add("nogvita-logo.png", LogoPng.Value, new ContentType("image", "png"));
            logo.ContentId = EmailLayout.LogoContentId;
        }

        mimeMessage.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        var security = settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(settings.Host, settings.Port, security, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
            await client.AuthenticateAsync(settings.Username, settings.Password!, cancellationToken);

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static byte[] LoadLogo()
    {
        using var stream = typeof(MailKitEmailSender).Assembly.GetManifestResourceStream("NogVita.Email.logo.png")
            ?? throw new InvalidOperationException("Recurso da logo de e-mail não encontrado.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}