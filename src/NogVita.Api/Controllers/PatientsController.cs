using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Patients;
using NogVita.Application.Photos;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>O perfil do paciente conectado.</summary>
[ApiController]
[Route("api/v1/patients")]
[Authorize(Roles = Roles.Patient)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class PatientsController(
    GetMyPatientProfileUseCase getMyPatientProfileUseCase,
    UpdateMyPatientProfileUseCase updateMyPatientProfileUseCase,
    ProfilePhotoUseCase profilePhotoUseCase) : ControllerBase
{
    /// <summary>Devolve o perfil do paciente: dados de saúde, objetivo e rotina.</summary>
    [HttpGet("me")]
    [ProducesResponseType<PatientProfileResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var response = await getMyPatientProfileUseCase.ExecuteAsync(User.GetUserId(), cancellationToken);

        if (response is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.");
        }

        return Ok(response);
    }

    /// <summary>Atualiza o perfil do paciente, inclusive a rotina.</summary>
    /// <remarks>
    /// Envie sempre os três horários da rotina (acordar, dormir e treinar): um horário omitido é apagado.
    /// O paciente precisa ter pelo menos 18 anos.
    /// </remarks>
    [HttpPut("me")]
    [ProducesResponseType<PatientProfileResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe(UpdatePatientProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await updateMyPatientProfileUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        if (response is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.");
        }

        return Ok(response);
    }

    /// <summary>Envia ou troca a foto de perfil do paciente.</summary>
    /// <remarks>Privada: a resposta traz um link temporário. A imagem é recodificada em WebP, sem os metadados (como a localização GPS).</remarks>
    [HttpPut("me/photo")]
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
        var outcome = await profilePhotoUseCase.UploadAsync(User.GetUserId(), ProfilePhotoOwner.Patient, content, cancellationToken);

        return this.ToActionResult(outcome);
    }

    /// <summary>Remove a foto de perfil do paciente, e apaga o arquivo.</summary>
    [HttpDelete("me/photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePhoto(CancellationToken cancellationToken)
    {
        var result = await profilePhotoUseCase.RemoveAsync(User.GetUserId(), ProfilePhotoOwner.Patient, cancellationToken);

        return this.ToActionResult(new ProfilePhotoOutcome(result));
    }
}
