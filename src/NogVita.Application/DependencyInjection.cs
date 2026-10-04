using Microsoft.Extensions.DependencyInjection;
using NogVita.Application.Auth;

namespace NogVita.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, RefreshTokenSettings refreshTokenSettings)
    {
        services.AddScoped<LoginUseCase>();
        services.AddSingleton(refreshTokenSettings);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TokenIssuer>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUseCase>();

        return services;
    }
}