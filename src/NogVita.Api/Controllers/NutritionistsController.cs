using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Nutritionists;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>A lista de nutricionistas e o perfil do nutricionista conectado.</summary>
[ApiController]
[Route("api/v1/nutritionists")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistsController(
    INutritionistQueries nutritionistQueries,
    GetMyNutritionistProfileUseCase getMyProfileUseCase,
    UpdateMyNutritionistProfileUseCase updateMyProfileUseCase) : ControllerBase
{
    /// <summary>Lista os nutricionistas ativos, para o paciente escolher um.</summary>
    /// <remarks>Qualquer usuário conectado pode ver. A busca é pelo nome; o e-mail e o CPF nunca aparecem.</remarks>
    [HttpGet]
    [ProducesResponseType<PagedResponse<NutritionistListItem>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> List([FromQuery] NutritionistListRequest request, CancellationToken cancellationToken)
    {
        var response = await nutritionistQueries.ListAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>Devolve o perfil do nutricionista conectado (CRN e apresentação).</summary>
    [HttpGet("me")]
    [Authorize(Roles = Roles.Nutritionist)]
    [ProducesResponseType<NutritionistProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var response = await getMyProfileUseCase.ExecuteAsync(User.GetUserId(), cancellationToken);

        return response is null ? ProfileNotFound() : Ok(response);
    }

    /// <summary>Atualiza a apresentação do nutricionista (até 500 caracteres).</summary>
    /// <remarks>O CRN não muda por aqui: ele vem do pré-cadastro feito pelo administrador.</remarks>
    [HttpPut("me")]
    [Authorize(Roles = Roles.Nutritionist)]
    [ProducesResponseType<NutritionistProfileResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe(UpdateMyNutritionistProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await updateMyProfileUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        return response is null ? ProfileNotFound() : Ok(response);
    }

    private ObjectResult ProfileNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de nutricionista não encontrado.");
}
