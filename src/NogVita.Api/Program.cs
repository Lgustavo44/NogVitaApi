using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using NogVita.Api.Errors;
using NogVita.Api.Filters;
using NogVita.Api.OpenApi;
using NogVita.Application;
using NogVita.Application.Auth;
using NogVita.Application.Common;
using NogVita.Infrastructure;
using NogVita.Infrastructure.Email;
using NogVita.Infrastructure.Foods.OpenFoodFacts;
using NogVita.Infrastructure.Persistence.Seed;
using NogVita.Infrastructure.Security;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Sem o "Server: Kestrel" nas respostas: não há motivo para anunciar a tecnologia do servidor.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Toda resposta de erro no mesmo formato (ProblemDetails), inclusive as do tratador global (D167).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var connectionString = builder.Configuration.GetConnectionString("NogVita")
    ?? throw new InvalidOperationException("Connection string 'NogVita' não configurada.");

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? throw new InvalidOperationException("Jwt settings not configured.");
jwtSettings.Validate();

var emailSettings = builder.Configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>()
    ?? throw new InvalidOperationException("Seção 'Email' não configurada.");
emailSettings.Validate();

var openFoodFactsSettings = builder.Configuration
    .GetSection(OpenFoodFactsSettings.SectionName)
    .Get<OpenFoodFactsSettings>() ?? new OpenFoodFactsSettings();
openFoodFactsSettings.Validate();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

// Seguro por padrão (D162): toda rota exige login, a menos que declare [AllowAnonymous].
// Um controller novo que esquecer o [Authorize] fica fechado, e não aberto.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddInfrastructure(connectionString, jwtSettings, emailSettings, openFoodFactsSettings);
var refreshTokenSettings = builder.Configuration.GetSection(RefreshTokenSettings.SectionName).Get<RefreshTokenSettings>()
    ?? throw new InvalidOperationException("Seção 'RefreshToken' não configurada.");
refreshTokenSettings.Validate();

var frontendSettings = builder.Configuration.GetSection(FrontendSettings.SectionName).Get<FrontendSettings>()
    ?? throw new InvalidOperationException("Seção 'Frontend' não configurada.");
frontendSettings.Validate();

builder.Services.AddApplication(refreshTokenSettings);
builder.Services.AddSingleton(frontendSettings);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Atrás do proxy da Render, o TLS termina no proxy: sem isso, a API acha que a requisição é http e que todos os clientes têm o IP do proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Os IPs do proxy da Render não são fixos; a API só é acessível através dele.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// D181: nos testes de integração, todas as requisições vêm do mesmo "IP" e o limite barraria a suíte.
// Desligado por configuração (RateLimiting:Enabled = false), as políticas continuam existindo, só sem limite.
var rateLimitingEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", true);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
        !rateLimitingEnabled ? RateLimitPartition.GetNoLimiter("desligado") : RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("plan-email", httpContext =>
    !rateLimitingEnabled ? RateLimitPartition.GetNoLimiter("desligado") : RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User.FindFirst("sub")?.Value ?? "anonymous",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromHours(1)
        }));

    options.AddPolicy("food-lookup", httpContext =>
    !rateLimitingEnabled ? RateLimitPartition.GetNoLimiter("desligado") : RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
    var adminSeedSettings = app.Configuration
        .GetSection(AdminSeedSettings.SectionName)
        .Get<AdminSeedSettings>();

    await seeder.SeedAsync(adminSeedSettings);
    await scope.ServiceProvider.GetRequiredService<TacoFoodSeeder>().SeedAsync();
}

app.UseForwardedHeaders();

// Cabeçalhos de segurança, antes de qualquer outro middleware: valem também para os 401, 403 e 429.
// Gravados no OnStarting, logo antes de a resposta sair: o tratador de exceções limpa os cabeçalhos,
// e assim eles aparecem também nos 409, 422 e 500 gerados por ele.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        // Só na API: o Swagger é uma página de verdade e precisa carregar os scripts e os estilos dele.
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            headers["Cache-Control"] = "no-store";
        }

        return Task.CompletedTask;
    });

    await next();
});

// D167: o tratador global de exceções, logo depois dos cabeçalhos, para pegar o que vier de qualquer ponto do pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "NogVita v1");
    });
}

app.UseHttpsRedirection();

app.UseCors();

// O rate limiter vem depois da autenticação: a política "food-lookup" particiona pelo "sub" do token.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Público para os testes de integração (WebApplicationFactory<Program>).
public partial class Program;
