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
    ProfilePhotoUseCase profilePhotoUseCase,
    HealthDataConsentUseCase healthDataConsentUseCase) : ControllerBase
{
    /// <summary>Devolve o termo atual dos dados de saúde e da transferência internacional, para a tela de cadastro.</summary>
    /// <remarks>São dois aceites separados (LGPD, arts. 11, I, e 33, VIII). Envie a `version` deste termo no cadastro.</remarks>
    [HttpGet("health-data-consent")]
    [AllowAnonymous]
    [ProducesResponseType<HealthDataConsentTermResponse>(StatusCodes.Status200OK)]
    public HealthDataConsentTermResponse GetHealthDataConsentTerm() => HealthDataConsent.Term;

    /// <summary>Diz se o paciente aceitou a versão atual do termo dos dados de saúde.</summary>
    /// <remarks>Quem se cadastrou antes do termo, ou antes de uma versão nova, vem com `accepted: false` e precisa aceitar.</remarks>
    [HttpGet("me/health-data-consent")]
    [ProducesResponseType<HealthDataConsentResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHealthDataConsent(CancellationToken cancellationToken)
    {
        var response = await healthDataConsentUseCase.GetAsync(User.GetUserId(), cancellationToken);

        return response is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.")
            : Ok(response);
    }

    /// <summary>Aceita o termo dos dados de saúde e da transferência internacional, na versão informada (precisa ser a atual).</summary>
    [HttpPost("me/health-data-consent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcceptHealthDataConsent(AcceptHealthDataConsentRequest request, CancellationToken cancellationToken) =>
        await healthDataConsentUseCase.AcceptAsync(User.GetUserId(), request, cancellationToken) switch
        {
            HealthDataConsentResult.Done => NoContent(),
            HealthDataConsentResult.OutdatedVersion => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "O termo foi atualizado. Leia a versão nova e aceite de novo."),
            _ => Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.")
        };

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
