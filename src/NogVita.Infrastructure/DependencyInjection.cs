using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NogVita.Application.Abstractions;
using NogVita.Application.Admin;
using NogVita.Application.Auth;
using NogVita.Infrastructure.Persistence;
using NogVita.Infrastructure.Persistence.Repositories;
using NogVita.Infrastructure.Persistence.Seed;
using NogVita.Infrastructure.Queries;
using NogVita.Infrastructure.Security;

namespace NogVita.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, JwtSettings jwtSettings)
    {
        services.AddDbContext<NogVitaDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        //models
        services.AddHealthChecks().AddDbContextCheck<NogVitaDbContext>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<AdminSeeder>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAdminUserQueries, AdminUserQueries>();

        //jwt
        services.AddSingleton(jwtSettings);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<NogVitaDbContext>());
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }
}