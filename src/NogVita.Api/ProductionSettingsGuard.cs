namespace NogVita.Api;

// Fase 26: em produção, uma configuração esquecida não pode virar uma brecha silenciosa.
// A API se recusa a subir, e a mensagem diz exatamente o que falta (docs/dev/producao.md).
internal static class ProductionSettingsGuard
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var problems = new List<string>();

        // O padrão do appsettings.json é "*": em produção, só o domínio da API.
        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Split(';').Any(host => host.Trim() == "*"))
        {
            problems.Add("AllowedHosts precisa listar o domínio da API (e não \"*\").");
        }

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 || origins.Any(origin => !origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            problems.Add("Cors:AllowedOrigins precisa ter o endereço do front, só com https://.");
        }

        if (configuration["Frontend:BaseUrl"]?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) != true)
        {
            problems.Add("Frontend:BaseUrl precisa ser https://: os links dos e-mails levam tokens.");
        }

        if (!string.Equals(configuration["Email:Provider"], "BrevoApi", StringComparison.OrdinalIgnoreCase))
        {
            problems.Add("Email:Provider precisa ser BrevoApi (o SMTP local é só para desenvolvimento).");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuração de produção incompleta:" + Environment.NewLine + string.Join(Environment.NewLine, problems.Select(p => "- " + p)));
        }
    }
}
