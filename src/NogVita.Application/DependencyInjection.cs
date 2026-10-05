using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NogVita.Application.Auth;
using NogVita.Application.Patients;

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
        services.AddScoped<RegisterPatientUseCase>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}