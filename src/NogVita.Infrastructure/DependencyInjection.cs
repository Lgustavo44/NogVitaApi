using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NogVita.Application.Abstractions;
using NogVita.Infrastructure.Persistence;
using NogVita.Infrastructure.Persistence.Seed;
using NogVita.Infrastructure.Security;

namespace NogVita.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NogVitaDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        services.AddHealthChecks().AddDbContextCheck<NogVitaDbContext>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<AdminSeeder>();

        return services;
    }
}