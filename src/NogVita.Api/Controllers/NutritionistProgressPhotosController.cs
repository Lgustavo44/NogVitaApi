using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.ProgressPhotos;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>As fotos de evolução dos pacientes que o nutricionista acompanha.</summary>
[ApiController]
[Route("api/v1/nutritionists/me")]
[Authorize(Roles = Roles.Nutritionist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistProgressPhotosController(ProgressPhotoUseCase useCase) : ControllerBase
{
    /// <summary>Lista as fotos de evolução de um paciente acompanhado, agrupadas por data, com links temporários.</summary>
    /// <remarks>Só com vínculo ativo (D-V2-06): sem ele, 404.</remarks>
    [HttpGet("patients/{patientId:guid}/progress-photos")]
    [ProducesResponseType<IReadOnlyList<ProgressPhotoDayResponse>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(Guid patientId, CancellationToken cancellationToken) =>
        this.ToActionResult(await useCase.ListForNutritionistAsync(User.GetUserId(), patientId, cancellationToken));
}
