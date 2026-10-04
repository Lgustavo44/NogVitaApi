using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NogVita.Api.OpenApi;
using NogVita.Application;
using NogVita.Application.Auth;
using NogVita.Infrastructure;
using NogVita.Infrastructure.Persistence.Seed;
using NogVita.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

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

builder.Services.AddInfrastructure(connectionString, jwtSettings);
var refreshTokenSettings = builder.Configuration.GetSection(RefreshTokenSettings.SectionName).Get<RefreshTokenSettings>()
    ?? throw new InvalidOperationException("Seção 'RefreshToken' não configurada.");
refreshTokenSettings.Validate();

builder.Services.AddApplication(refreshTokenSettings);


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
    var adminSeedSettings = app.Configuration
        .GetSection(AdminSeedSettings.SectionName)
        .Get<AdminSeedSettings>();

    await seeder.SeedAsync(adminSeedSettings);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "NogVita v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
