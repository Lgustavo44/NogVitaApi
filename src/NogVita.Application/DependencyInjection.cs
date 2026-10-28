using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NogVita.Application.Admin;
using NogVita.Application.Auth;
using NogVita.Application.FollowUps;
using NogVita.Application.Foods;
using NogVita.Application.Nutritionists;
using NogVita.Application.NutritionPlans;
using NogVita.Application.Patients;
using NogVita.Application.WeightRecords;

namespace NogVita.Application;

// Um bloco por pasta (namespace) da Application. Em cada bloco, primeiro os serviços de apoio, depois os casos de uso.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, RefreshTokenSettings refreshTokenSettings)
    {
        // Compartilhados
        services.AddSingleton(refreshTokenSettings);
        services.AddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Autenticação e senha (Auth)
        services.AddScoped<TokenIssuer>();
        services.AddScoped<PasswordResetNotificationService>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUseCase>();
        services.AddScoped<RequestPasswordResetUseCase>();
        services.AddScoped<ConfirmPasswordResetUseCase>();

        // Pacientes e confirmação de e-mail (Patients)
        services.AddScoped<EmailConfirmationService>();
        services.AddScoped<RegisterPatientUseCase>();
        services.AddScoped<ConfirmEmailUseCase>();
        services.AddScoped<ResendEmailConfirmationUseCase>();
        services.AddScoped<GetMyPatientProfileUseCase>();
        services.AddScoped<UpdateMyPatientProfileUseCase>();

        // Nutricionistas e convites (Nutritionists)
        services.AddScoped<NutritionistInvitationService>();
        services.AddScoped<PreRegisterNutritionistUseCase>();
        services.AddScoped<ResendNutritionistInvitationUseCase>();
        services.AddScoped<AcceptNutritionistInvitationUseCase>();
        services.AddScoped<CancelNutritionistPreRegistrationUseCase>();
        services.AddScoped<GetMyNutritionistProfileUseCase>();
        services.AddScoped<UpdateMyNutritionistProfileUseCase>();

        // Administração de contas (Admin)
        services.AddSingleton<AdminAuditLog>();
        services.AddScoped<ActivateUserUseCase>();
        services.AddScoped<DeactivateUserUseCase>();

        // Acompanhamento nutricional (FollowUps)
        services.AddScoped<CareAccessService>();
        services.AddScoped<FollowUpNotificationService>();
        services.AddScoped<RequestNutritionistUseCase>();
        services.AddScoped<CancelNutritionistRequestUseCase>();
        services.AddScoped<RespondNutritionistRequestUseCase>();
        services.AddScoped<GetMyCareRelationshipUseCase>();
        services.AddScoped<GetFollowedPatientUseCase>();
        services.AddScoped<EndCareRelationshipUseCase>();

        // Base de alimentos (Foods)
        services.AddScoped<LookupProductByBarcodeUseCase>();
        services.AddScoped<ImportProductUseCase>();
        services.AddScoped<SetFoodActiveUseCase>();

        // Planos, refeições e PDF (NutritionPlans)
        services.AddScoped<NutritionistPlanAccess>();
        services.AddScoped<CreateNutritionPlanUseCase>();
        services.AddScoped<ManageNutritionPlanUseCase>();
        services.AddScoped<NutritionistPlanQueriesUseCase>();
        services.AddScoped<ManageMealsUseCase>();
        services.AddScoped<ManageMealFoodsUseCase>();
        services.AddScoped<PlanPdfUseCase>();

        // Registro de peso (WeightRecords)
        services.AddScoped<RecordWeightUseCase>();
        services.AddScoped<DeleteWeightRecordUseCase>();
        services.AddScoped<NutritionistWeightQueriesUseCase>();

        return services;
    }
}
