using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.Extensions;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/patients/me/nutrition-plans")]
[Authorize(Roles = Roles.Patient)]
public sealed class PatientNutritionPlansController(INutritionPlanQueries planQueries, PlanPdfUseCase planPdfUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] NutritionPlanListRequest request, CancellationToken cancellationToken) =>
        Ok(await planQueries.ListByPatientAsync(User.GetUserId(), request, cancellationToken));

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var plan = await planQueries.GetCurrentByPatientAsync(User.GetUserId(), cancellationToken);

        return plan is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Você não tem um plano ativo.")
            : Ok(plan);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var plan = await planQueries.GetByPatientAsync(User.GetUserId(), id, cancellationToken);

        return plan is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Plano não encontrado.")
            : Ok(plan);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var file = await planPdfUseCase.RenderForPatientAsync(User.GetUserId(), id, cancellationToken);

        return file is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Plano não encontrado.")
            : File(file.Content, "application/pdf", file.FileName);
    }

    [HttpPost("{id:guid}/pdf/email")]
    [EnableRateLimiting("plan-email")]
    public async Task<IActionResult> EmailPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await planPdfUseCase.EmailToPatientAsync(User.GetUserId(), id, cancellationToken);

        return result switch
        {
            EmailPlanPdfResult.Sent => NoContent(),
            EmailPlanPdfResult.EmailFailed => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Não foi possível enviar o e-mail agora. Tente novamente em alguns minutos, ou baixe o PDF."),
            EmailPlanPdfResult.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Plano não encontrado.")
        };
    }
}