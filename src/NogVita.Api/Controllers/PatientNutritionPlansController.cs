using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common.Pagination;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>Os planos alimentares, do lado do paciente.</summary>
/// <remarks>O paciente vê só os planos ativos e finalizados, inclusive os de nutricionistas anteriores. Rascunhos e cancelados nunca aparecem.</remarks>
[ApiController]
[Route("api/v1/patients/me/nutrition-plans")]
[Authorize(Roles = Roles.Patient)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class PatientNutritionPlansController(INutritionPlanQueries planQueries, PlanPdfUseCase planPdfUseCase) : ControllerBase
{
    /// <summary>Lista os planos do paciente: o ativo primeiro, depois os finalizados, do mais recente para o mais antigo.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<NutritionPlanResponse>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> List([FromQuery] NutritionPlanListRequest request, CancellationToken cancellationToken) =>
        Ok(await planQueries.ListByPatientAsync(User.GetUserId(), request, cancellationToken));

    /// <summary>Devolve o plano ativo, com as refeições, os alimentos e os totais calculados.</summary>
    /// <remarks>O 404 ("Você não tem um plano ativo.") não é um erro de verdade: é o estado de quem ainda não recebeu um plano.</remarks>
    [HttpGet("current")]
    [ProducesResponseType<NutritionPlanDetailResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var plan = await planQueries.GetCurrentByPatientAsync(User.GetUserId(), cancellationToken);

        return plan is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Você não tem um plano ativo.")
            : Ok(plan);
    }

    /// <summary>Devolve um plano do paciente, com as refeições, os alimentos e os totais calculados.</summary>
    /// <param name="id">O Id do plano.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<NutritionPlanDetailResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var plan = await planQueries.GetByPatientAsync(User.GetUserId(), id, cancellationToken);

        return plan is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Plano não encontrado.")
            : Ok(plan);
    }

    /// <summary>Baixa o plano em PDF.</summary>
    /// <param name="id">O Id do plano.</param>
    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var file = await planPdfUseCase.RenderForPatientAsync(User.GetUserId(), id, cancellationToken);

        return file is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Plano não encontrado.")
            : File(file.Content, "application/pdf", file.FileName);
    }

    /// <summary>Envia o plano em PDF para o e-mail do paciente.</summary>
    /// <remarks>No máximo 3 envios por hora, por paciente (429 depois disso). Se o envio falhar, a resposta é 503: dá para baixar o PDF.</remarks>
    /// <param name="id">O Id do plano.</param>
    [HttpPost("{id:guid}/pdf/email")]
    [EnableRateLimiting("plan-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesProblem(StatusCodes.Status503ServiceUnavailable)]
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
