using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/patients/me/nutrition-plans")]
[Authorize(Roles = Roles.Patient)]
public sealed class PatientNutritionPlansController(INutritionPlanQueries planQueries) : ControllerBase
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
}