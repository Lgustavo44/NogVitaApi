namespace NogVita.Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Jwt: Issuer e Audience são obrigatórios.");

        if (ExpirationMinutes <= 0)
            throw new InvalidOperationException("Jwt: ExpirationMinutes precisa ser maior que zero.");

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(SecretKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Jwt: SecretKey precisa estar em base64.");
        }

        if (keyBytes.Length < 32)
            throw new InvalidOperationException("Jwt: SecretKey precisa ter no mínimo 32 bytes.");
    }
}