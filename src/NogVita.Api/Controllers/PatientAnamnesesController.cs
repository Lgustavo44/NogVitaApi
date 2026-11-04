using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Anamneses;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>As anamneses do paciente: as pendentes, para responder, e as já respondidas.</summary>
/// <remarks>O paciente lê as anamneses dele mesmo depois de o acompanhamento acabar.</remarks>
[ApiController]
[Route("api/v1/patients/me/anamneses")]
[Authorize(Roles = Roles.Patient)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class PatientAnamnesesController(IAnamnesisQueries anamnesisQueries, AnamnesisUseCase anamnesisUseCase) : ControllerBase
{
    /// <summary>Lista as anamneses do paciente: as pendentes primeiro, depois as respondidas, da mais recente para a mais antiga.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<AnamnesisSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> List([FromQuery] AnamnesisListRequest request, CancellationToken cancellationToken) =>
        Ok(await anamnesisQueries.ListByPatientAsync(User.GetUserId(), request, cancellationToken));

    /// <summary>Devolve a anamnese, com as perguntas e, se já respondida, as respostas.</summary>
    /// <param name="id">O Id da anamnese.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AnamnesisResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var anamnesis = await anamnesisQueries.GetByPatientAsync(User.GetUserId(), id, cancellationToken);

        return anamnesis is null ? AnamnesisNotFound() : Ok(anamnesis);
    }

    /// <summary>Envia as respostas. Depois disso, a anamnese fica travada.</summary>
    /// <remarks>Exige o aceite do termo dos dados de saúde (422 sem ele). Uma pergunta sem resposta fica de fora; as obrigatórias, não.</remarks>
    /// <param name="id">O Id da anamnese.</param>
    [HttpPost("{id:guid}/answers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit(Guid id, SubmitAnamnesisRequest request, CancellationToken cancellationToken) =>
        await anamnesisUseCase.SubmitAsync(User.GetUserId(), id, request, cancellationToken) switch
        {
            SubmitAnamnesisResult.Submitted => NoContent(),
            SubmitAnamnesisResult.AlreadySubmitted => Problem(
                statusCode: StatusCodes.Status409Conflict, title: "Esta anamnese já foi respondida."),
            SubmitAnamnesisResult.NoLongerAvailable => Problem(
                statusCode: StatusCodes.Status409Conflict, title: "Este questionário não está mais disponível."),
            SubmitAnamnesisResult.NotFound => AnamnesisNotFound()
        };

    private ObjectResult AnamnesisNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Anamnese não encontrada.");
}
