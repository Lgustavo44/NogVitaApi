namespace NogVita.Infrastructure.Foods.OpenFoodFacts;

public sealed class OpenFoodFactsSettings
{
    public const string SectionName = "OpenFoodFacts";

    public string BaseUrl { get; init; } = "https://world.openfoodfacts.org/";
    public string UserAgent { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 10;

    public void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("OpenFoodFacts: BaseUrl precisa ser uma URL https absoluta.");
        }

        if (string.IsNullOrWhiteSpace(UserAgent) || !UserAgent.Contains('@'))
        {
            throw new InvalidOperationException("OpenFoodFacts: UserAgent é obrigatório e precisa ter um e-mail de contato, no formato \"NogVita/1.0 (email@dominio.com)\".");
        }

        if (TimeoutSeconds is < 1 or > 60)
        {
            throw new InvalidOperationException("OpenFoodFacts: TimeoutSeconds precisa estar entre 1 e 60.");
        }
    }
}