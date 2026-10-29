namespace NogVita.Infrastructure.Persistence.Seed;

// D192: a marca de uma importação em massa que terminou, com o hash do arquivo importado.
// Na próxima subida, o mesmo hash quer dizer "nada a fazer": a importação não relê o arquivo.
public sealed class SeedRun
{
    public string Name { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public DateTime CompletedAtUtc { get; private set; }

    private SeedRun() { } // EF Core

    public SeedRun(string name, string contentHash, DateTime completedAtUtc)
    {
        Name = name;
        ContentHash = contentHash;
        CompletedAtUtc = completedAtUtc;
    }

    public void Complete(string contentHash, DateTime completedAtUtc)
    {
        ContentHash = contentHash;
        CompletedAtUtc = completedAtUtc;
    }
}
