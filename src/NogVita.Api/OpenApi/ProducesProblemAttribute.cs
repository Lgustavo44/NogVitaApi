using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NogVita.Api.OpenApi;

// D184: um erro com corpo ProblemDetails (title, status, traceId). Ex.: [ProducesProblem(StatusCodes.Status404NotFound)].
public sealed class ProducesProblemAttribute(int statusCode)
    : ProducesResponseTypeAttribute(typeof(ProblemDetails), statusCode, "application/problem+json");

// O 400 do ValidationFilter: o ProblemDetails com o objeto "errors", campo por campo.
public sealed class ProducesValidationProblemAttribute()
    : ProducesResponseTypeAttribute(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json");
