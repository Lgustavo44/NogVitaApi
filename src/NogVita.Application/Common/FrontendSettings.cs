namespace NogVita.Application.Common;

public sealed class FrontendSettings
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; init; } = string.Empty;

    public void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Frontend: BaseUrl precisa ser uma URL absoluta (http ou https).");
    }
}