using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.OpenApi;
using NogVita.Application.Admin;
using NogVita.Application.Common;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>O resumo do início do administrador.</summary>
[ApiController]
[Route("api/v1/admin/overview")]
[Authorize(Roles = Roles.Admin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class AdminOverviewController(IAdminUserQueries adminUserQueries, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>Devolve os números do uso do diário e as pendências da plataforma.</summary>
    /// <remarks>
    /// Os pacientes ativos sem o aceite da versão atual do termo dos dados de saúde, os dias com diário nos últimos
    /// 7 dias, os check-ins semanais sem devolutiva e quantos alimentos ativos já têm porções.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<AdminOverviewResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await adminUserQueries.GetOverviewAsync(BrazilianCalendar.Today(timeProvider).AddDays(-6), cancellationToken));
}
