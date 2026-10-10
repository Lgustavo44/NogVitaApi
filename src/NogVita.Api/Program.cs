using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
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

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

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

builder.Services.AddAuthorization();

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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("food-lookup", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
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
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.MapOpenApi();
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
app.MapHealthChecks("/health");

app.Run();
