using NogVita.Application.Abstractions;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeEmailSender : IEmailSender
{
    public List<EmailMessage> SentMessages { get; } = [];
    public bool ShouldFail { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (ShouldFail)
            throw new InvalidOperationException("Falha simulada no envio de e-mail.");

        SentMessages.Add(message);
        return Task.CompletedTask;
    }
}