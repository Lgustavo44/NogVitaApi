namespace NogVita.Infrastructure.Email;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public bool UseStartTls { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host) || Port <= 0)
            throw new InvalidOperationException("Email: Host e Port são obrigatórios.");

        if (string.IsNullOrWhiteSpace(FromAddress))
            throw new InvalidOperationException("Email: FromAddress é obrigatório.");

        if (!string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("Email: Password é obrigatório quando Username é informado.");
    }
}