using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Nutritionists;
using NogVita.Application.Photos;
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
    UpdateMyNutritionistProfileUseCase updateMyProfileUseCase,
    ProfilePhotoUseCase profilePhotoUseCase) : ControllerBase
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

    /// <summary>Envia ou troca a foto de perfil do nutricionista.</summary>
    /// <remarks>Pública: aparece na busca de nutricionistas. A imagem é recodificada em WebP, sem os metadados (como a localização GPS).</remarks>
    [HttpPut("me/photo")]
    [Authorize(Roles = Roles.Nutritionist)]
    [EnableRateLimiting("photo-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProfilePhotoResults.MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProfilePhotoResults.MaxUploadBytes)]
    [ProducesResponseType<ProfilePhotoResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var outcome = await profilePhotoUseCase.UploadAsync(User.GetUserId(), ProfilePhotoOwner.Nutritionist, content, cancellationToken);

        return this.ToActionResult(outcome);
    }

    /// <summary>Remove a foto de perfil do nutricionista, e apaga o arquivo.</summary>
    [HttpDelete("me/photo")]
    [Authorize(Roles = Roles.Nutritionist)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePhoto(CancellationToken cancellationToken)
    {
        var result = await profilePhotoUseCase.RemoveAsync(User.GetUserId(), ProfilePhotoOwner.Nutritionist, cancellationToken);

        return this.ToActionResult(new ProfilePhotoOutcome(result));
    }
}
