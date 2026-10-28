using System.Collections.Concurrent;
using NogVita.Application.Abstractions;

namespace NogVita.IntegrationTests.Infrastructure;

// D180: os e-mails são capturados, e não enviados. O teste lê o link do e-mail, como uma pessoa faria.
// Como o banco e a API são compartilhados por todos os testes (D178), cada teste procura só os e-mails
// do endereço que ele mesmo criou (D179).
public sealed class CapturedEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> SentTo(string address) =>
        _messages.Where(m => string.Equals(m.To, address, StringComparison.OrdinalIgnoreCase)).ToList();
}
