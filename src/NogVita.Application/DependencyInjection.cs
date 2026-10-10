using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NogVita.Application.Admin;
using NogVita.Application.Auth;
using NogVita.Application.FollowUps;
using NogVita.Application.Foods;
using NogVita.Application.Nutritionists;
using NogVita.Application.Patients;

namespace NogVita.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, RefreshTokenSettings refreshTokenSettings)
    {
        // Configurações e serviços compartilhados
        services.AddSingleton(refreshTokenSettings);
        services.AddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Autenticação
        services.AddScoped<TokenIssuer>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUseCase>();

        // Pacientes
        services.AddScoped<RegisterPatientUseCase>();
        services.AddScoped<GetMyPatientProfileUseCase>();
        services.AddScoped<UpdateMyPatientProfileUseCase>();
        services.AddScoped<EmailConfirmationService>();

        // Nutricionistas
        services.AddScoped<NutritionistInvitationService>();
        services.AddScoped<PreRegisterNutritionistUseCase>();
        services.AddScoped<ResendNutritionistInvitationUseCase>();
        services.AddScoped<PreRegisterNutritionistUseCase>();
        services.AddScoped<AcceptNutritionistInvitationUseCase>();
        services.AddScoped<RespondNutritionistRequestUseCase>();
        services.AddScoped<GetMyNutritionistProfileUseCase>();
        services.AddScoped<UpdateMyNutritionistProfileUseCase>();

        // Admin
        services.AddScoped<DeactivateUserUseCase>();
        services.AddScoped<ActivateUserUseCase>();
        services.AddScoped<ConfirmEmailUseCase>();
        services.AddScoped<ResendEmailConfirmationUseCase>();

        // Follow-ups
        services.AddScoped<FollowUpNotificationService>();
        services.AddScoped<RequestNutritionistUseCase>();
        services.AddScoped<CancelNutritionistRequestUseCase>();
        services.AddScoped<GetMyCareRelationshipUseCase>();
        services.AddScoped<EndCareRelationshipUseCase>();

        // Foods
        services.AddScoped<LookupProductByBarcodeUseCase>();
        services.AddScoped<ImportProductUseCase>();
        services.AddScoped<SetFoodActiveUseCase>();


        return services;
    }
}