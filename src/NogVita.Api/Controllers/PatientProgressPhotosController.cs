using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.ProgressPhotos;
using NogVita.Domain.ProgressPhotos;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>As fotos de evolução do paciente conectado, e o termo de consentimento delas.</summary>
[ApiController]
[Route("api/v1/patients/me/progress-photos")]
[Authorize(Roles = Roles.Patient)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class PatientProgressPhotosController(ProgressPhotoUseCase useCase) : ControllerBase
{
    /// <summary>Devolve a versão atual do termo de consentimento das fotos de evolução, e se o paciente já a aceitou.</summary>
    [HttpGet("consent")]
    [ProducesResponseType<ProgressPhotoConsentResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsent(CancellationToken cancellationToken) =>
        this.ToActionResult(await useCase.GetConsentAsync(User.GetUserId(), cancellationToken));

    /// <summary>Aceita o termo de consentimento, na versão informada (precisa ser a atual).</summary>
    [HttpPost("consent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcceptConsent(AcceptProgressPhotoConsentRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await useCase.AcceptConsentAsync(User.GetUserId(), request.Version, cancellationToken));

    /// <summary>Revoga o termo e apaga todas as fotos de evolução (LGPD).</summary>
    [HttpDelete("consent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeConsent(CancellationToken cancellationToken) =>
        this.ToActionResult(await useCase.RevokeConsentAsync(User.GetUserId(), cancellationToken));

    /// <summary>Envia uma foto de evolução: a data, o ângulo e o arquivo. Reenviar o mesmo ângulo na mesma data substitui a foto.</summary>
    /// <remarks>Exige o termo aceito. A imagem é recodificada em WebP, sem os metadados (como a localização GPS).</remarks>
    [HttpPost]
    [EnableRateLimiting("photo-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProfilePhotoResults.MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProfilePhotoResults.MaxUploadBytes)]
    [ProducesResponseType<ProgressPhotoResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Upload([FromForm] UploadProgressPhotoForm form, CancellationToken cancellationToken)
    {
        await using var content = form.File.OpenReadStream();
        var outcome = await useCase.UploadAsync(User.GetUserId(), form.TakenOn, form.Angle, content, cancellationToken);

        return this.ToActionResult(outcome);
    }

    /// <summary>Lista as fotos de evolução do paciente, agrupadas por data, com links temporários.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProgressPhotoDayResponse>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        this.ToActionResult(await useCase.ListMineAsync(User.GetUserId(), cancellationToken));

    /// <summary>Apaga uma foto de evolução, e o arquivo.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await useCase.DeleteAsync(User.GetUserId(), id, cancellationToken));
}

public sealed class UploadProgressPhotoForm
{
    // Não anulável: com o [ApiController], um envio sem arquivo vira 400 com o erro no campo "File".
    public IFormFile File { get; init; } = null!;
    public DateOnly TakenOn { get; init; }
    public ProgressPhotoAngle Angle { get; init; }
}
