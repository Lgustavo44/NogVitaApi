using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace NogVita.Api.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;

        foreach (var (name, argument) in context.ActionArguments)
        {
            if (argument is null)
            {
                continue;
            }

            AddNullCharacterErrors(context, name, argument);

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            var problemDetailsFactory = services.GetRequiredService<ProblemDetailsFactory>();
            var problem = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);
            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }

    private const string NullCharacterMessage = "O texto contém um caractere inválido.";

    // O PostgreSQL não aceita o caractere nulo (\0) em textos: sem esta checagem, ele chegaria ao banco e viraria 500.
    // Vale para os textos da query e da rota e para as propriedades de texto do corpo.
    private static void AddNullCharacterErrors(ActionExecutingContext context, string name, object argument)
    {
        if (argument is string text)
        {
            if (text.Contains('\0'))
            {
                context.ModelState.AddModelError(name, NullCharacterMessage);
            }

            return;
        }

        foreach (var property in argument.GetType().GetProperties())
        {
            if (property.PropertyType == typeof(string)
                && property.GetIndexParameters().Length == 0
                && property.GetValue(argument) is string value
                && value.Contains('\0'))
            {
                context.ModelState.AddModelError(property.Name, NullCharacterMessage);
            }
        }
    }
}
