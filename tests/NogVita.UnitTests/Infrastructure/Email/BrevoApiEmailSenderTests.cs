using System.Net;
using System.Text.Json;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Infrastructure.Email;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Infrastructure.Email;

public class BrevoApiEmailSenderTests
{
    private const string LogoUrl = "https://exemplo.test/logo.png";

    private string? _sentJson;

    private BrevoApiEmailSender CreateSender()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            // Lido aqui dentro: depois do envio, o conteúdo da requisição pode não estar mais disponível.
            _sentJson = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.brevo.test/") };
        var settings = new EmailSettings
        {
            Provider = "BrevoApi",
            ApiKey = "chave-de-teste",
            FromAddress = "naoresponda@nogvita.test",
            FromName = "NogVita",
            LogoUrl = LogoUrl
        };

        return new BrevoApiEmailSender(httpClient, settings);
    }

    private static EmailMessage Message(IReadOnlyList<EmailAttachment>? attachments = null) =>
        new("paciente@nogvita.test", "Assunto", $"<img src=\"cid:{EmailLayout.LogoContentId}\">", "Texto", attachments);

    [Fact]
    public async Task Should_Replace_Inline_Logo_With_Public_Url()
    {
        await CreateSender().SendAsync(Message(), TestContext.Current.CancellationToken);

        var html = JsonDocument.Parse(_sentJson!).RootElement.GetProperty("htmlContent").GetString();

        Assert.Contains(LogoUrl, html);
        Assert.DoesNotContain("cid:", html);
    }

    [Fact]
    public async Task Should_Send_Attachments_As_Base64()
    {
        var pdf = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"

        await CreateSender().SendAsync(
            Message([new EmailAttachment("plano.pdf", "application/pdf", pdf)]),
            TestContext.Current.CancellationToken);

        var attachment = Assert.Single(JsonDocument.Parse(_sentJson!).RootElement.GetProperty("attachment").EnumerateArray());
        Assert.Equal("plano.pdf", attachment.GetProperty("name").GetString());
        Assert.Equal(Convert.ToBase64String(pdf), attachment.GetProperty("content").GetString());
    }

    [Fact]
    public async Task Should_Omit_Attachment_Field_Without_Attachments()
    {
        await CreateSender().SendAsync(Message(), TestContext.Current.CancellationToken);

        Assert.False(JsonDocument.Parse(_sentJson!).RootElement.TryGetProperty("attachment", out _));
    }
}
