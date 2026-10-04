namespace NogVita.Infrastructure.Persistence.Seed;

public sealed class AdminSeedSettings
{
    public const string SectionName = "AdminSeed";

    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Cpf { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}