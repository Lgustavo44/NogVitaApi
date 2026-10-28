using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NogVita.Application.Common;
using NogVita.Domain.Common;

namespace NogVita.Api.Errors;

// D167: a última linha de defesa. Toda exceção que nenhum caso de uso tratou chega aqui e vira um ProblemDetails.
// D168: conflito → 409, regra do domínio → 422, o resto → 500 genérico, com o detalhe só no log.
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public const string UnexpectedErrorTitle = "Ocorreu um erro inesperado. Tente novamente em instantes.";

    // Não é um código HTTP oficial: é o que o nginx usa quando o cliente desiste da requisição.
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = Classify(httpContext, exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            // O traceId da resposta é o mesmo deste log: com ele, dá para achar o erro que o usuário viu.
            logger.LogError(exception, "Erro não tratado em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("{Method} {Path} respondeu {Status}: {ExceptionType}.",
                httpContext.Request.Method, httpContext.Request.Path, status, exception.GetType().Name);
        }

        if (status == ClientClosedRequest)
        {
            // O cliente já foi embora: não há para quem escrever a resposta.
            httpContext.Response.StatusCode = status;
            return true;
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
    }

    public static (int Status, string Title) Classify(HttpContext httpContext, Exception exception) => exception switch
    {
        ConflictException conflict => (StatusCodes.Status409Conflict, conflict.Message),
        DomainException domain => (StatusCodes.Status422UnprocessableEntity, domain.Message),
        OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (ClientClosedRequest, "Requisição cancelada."),
        _ => (StatusCodes.Status500InternalServerError, UnexpectedErrorTitle)
    };
}
