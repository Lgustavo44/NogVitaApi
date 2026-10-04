namespace NogVita.Application.Auth;

public sealed class RefreshTokenSettings
{
    public const string SectionName = "RefreshToken";

    public int ExpirationDays { get; init; }

    public void Validate()
    {
        if (ExpirationDays <= 0)
            throw new InvalidOperationException("RefreshToken: ExpirationDays precisa ser maior que zero.");
    }
}