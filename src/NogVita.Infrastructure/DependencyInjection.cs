using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NogVita.Infrastructure.Persistence;

namespace NogVita.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NogVitaDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        return services;
    }
}