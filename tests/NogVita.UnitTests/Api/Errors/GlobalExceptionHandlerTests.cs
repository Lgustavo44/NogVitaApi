using Microsoft.AspNetCore.Http;
using NogVita.Api.Errors;
using NogVita.Application.Common;
using NogVita.Domain.Common;

namespace NogVita.UnitTests.Api.Errors;

public class GlobalExceptionHandlerTests
{
    private static readonly HttpContext Context = new DefaultHttpContext();

    [Fact]
    public void Conflict_Should_Be_409_With_Its_Message()
    {
        var (status, title) = GlobalExceptionHandler.Classify(Context, new ConflictException("Você já tem uma solicitação pendente."));

        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal("Você já tem uma solicitação pendente.", title);
    }

    [Fact]
    public void Domain_Rule_Should_Be_422_With_Its_Message()
    {
        var (status, title) = GlobalExceptionHandler.Classify(Context, new DomainException("Só um plano ativo pode ser finalizado."));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, status);
        Assert.Equal("Só um plano ativo pode ser finalizado.", title);
    }

    [Fact]
    public void Unexpected_Error_Should_Be_500_Without_Internal_Details()
    {
        var (status, title) = GlobalExceptionHandler.Classify(
            Context, new InvalidOperationException("Connection string com a senha do banco"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal(GlobalExceptionHandler.UnexpectedErrorTitle, title);
        Assert.DoesNotContain("senha", title);
    }

    [Fact]
    public void Cancellation_Without_Aborted_Request_Should_Still_Be_500()
    {
        // Um timeout interno também lança OperationCanceledException: só é "cliente desistiu" se a requisição foi abortada.
        var (status, _) = GlobalExceptionHandler.Classify(Context, new OperationCanceledException());

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
    }
}
