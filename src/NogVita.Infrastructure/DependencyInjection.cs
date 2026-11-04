using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Admin;
using NogVita.Application.Anamneses;
using NogVita.Application.Auth;
using NogVita.Application.FollowUps;
using NogVita.Application.Foods;
using NogVita.Application.Nutritionists;
using NogVita.Application.NutritionPlans;
using NogVita.Application.WeightRecords;
using NogVita.Infrastructure.Email;
using NogVita.Infrastructure.Foods.OpenFoodFacts;
using NogVita.Infrastructure.Images;
using NogVita.Infrastructure.Pdf;
using NogVita.Infrastructure.Persistence;
using NogVita.Infrastructure.Persistence.Queries;
using NogVita.Infrastructure.Persistence.Repositories;
using NogVita.Infrastructure.Persistence.Seed;
using NogVita.Infrastructure.Security;
using NogVita.Infrastructure.Storage;

namespace NogVita.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, JwtSettings jwtSettings, EmailSettings emailSettings, OpenFoodFactsSettings openFoodFactsSettings, StorageSettings storageSettings)
    {
        services.AddDbContext<NogVitaDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention()
                   // Uma falha ao salvar já chega ao tratador global: um índice único vira 409 (sem ser erro),
                   // e o resto vira 500, logado com a exceção completa. O log do próprio EF só duplicaria.
                   .ConfigureWarnings(warnings => warnings.Log(
                       (CoreEventId.SaveChangesFailed, LogLevel.Debug),
                       (RelationalEventId.CommandError, LogLevel.Debug))));

        //models
        // Fase 26: a checagem do banco tem a marca "ready" (o /health/ready a usa; o /health/live, não).
        // E a checagem das migrations: com alguma pendente, a API não está pronta (o banco não tem o que o código usa).
        services.AddHealthChecks()
            .AddDbContextCheck<NogVitaDbContext>(tags: ["ready"])
            .AddCheck<PendingMigrationsHealthCheck>("migrations", tags: ["ready"]);
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<AdminSeeder>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<INutritionistQueries, NutritionistQueries>();
        services.AddScoped<IAdminUserQueries, AdminUserQueries>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<INutritionistRequestRepository, NutritionistRequestRepository>();
        services.AddScoped<ICareRelationshipRepository, CareRelationshipRepository>();
        services.AddScoped<IFollowUpQueries, FollowUpQueries>();
        services.AddScoped<IFoodRepository, FoodRepository>();
        services.AddScoped<IFoodQueries, FoodQueries>();
        services.AddScoped<TacoFoodSeeder>();
        services.AddScoped<OpenFoodFactsBulkSeeder>();
        // D181: os testes de integração desligam a importação dos 22 mil produtos (OpenFoodFacts:ImportOnStartup = false).
        if (openFoodFactsSettings.ImportOnStartup)
        {
            services.AddHostedService<OpenFoodFactsImportHostedService>();
        }
        services.AddScoped<INutritionPlanRepository, NutritionPlanRepository>();
        services.AddScoped<INutritionPlanQueries, NutritionPlanQueries>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddSingleton<INutritionPlanPdfRenderer, QuestPdfNutritionPlanRenderer>();
        services.AddScoped<IWeightRecordRepository, WeightRecordRepository>();
        services.AddScoped<IPlanDocumentRepository, PlanDocumentRepository>();
        services.AddScoped<IAdminAuditRepository, AdminAuditRepository>();
        services.AddScoped<IWeightRecordQueries, WeightRecordQueries>();
        services.AddScoped<IProgressPhotoRepository, ProgressPhotoRepository>();
        services.AddScoped<IQuestionnaireRepository, QuestionnaireRepository>();
        services.AddScoped<IAnamnesisRepository, AnamnesisRepository>();
        services.AddScoped<IAnamnesisQueries, AnamnesisQueries>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddSingleton(emailSettings);
        services.AddSingleton(storageSettings);

        if (storageSettings.UsesSupabase)
        {
            services.AddHttpClient<IFileStorage, SupabaseFileStorage>(client =>
            {
                client.BaseAddress = new Uri(storageSettings.Url!.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("apikey", storageSettings.SecretKey);

                // As chaves novas (sb_secret_...) não são JWT: vão só no apikey.
                // A chave antiga (service_role) é um JWT, e vai também no Authorization.
                if (!storageSettings.SecretKey!.StartsWith("sb_secret_", StringComparison.Ordinal))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", storageSettings.SecretKey);
                }

                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }
        else
        {
            services.AddSingleton<IFileStorage, InMemoryFileStorage>();
        }

        if (emailSettings.UsesBrevoApi)
        {
            services.AddHttpClient<IEmailSender, BrevoApiEmailSender>(client =>
            {
                client.BaseAddress = new Uri("https://api.brevo.com/");
                client.DefaultRequestHeaders.Add("api-key", emailSettings.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(10);
            });
        }
        else
        {
            services.AddScoped<IEmailSender, MailKitEmailSender>();
        }

        services.AddHttpClient<IProductCatalog, OpenFoodFactsProductCatalog>(client =>
        {
            client.BaseAddress = new Uri(openFoodFactsSettings.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(openFoodFactsSettings.UserAgent);
            client.Timeout = TimeSpan.FromSeconds(openFoodFactsSettings.TimeoutSeconds);
        });


        //jwt
        services.AddSingleton(jwtSettings);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<NogVitaDbContext>());
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }
}
