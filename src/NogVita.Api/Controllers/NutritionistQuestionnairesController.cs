using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Anamneses;
using NogVita.Application.Common;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>Os questionários de anamnese do nutricionista.</summary>
/// <remarks>
/// Cada nutricionista monta os próprios questionários, a partir do modelo base ou do zero. Editar ou apagar um
/// questionário não muda as anamneses já enviadas: cada uma guarda a cópia das perguntas que o paciente recebeu.
/// </remarks>
[ApiController]
[Route("api/v1/nutritionists/me/questionnaires")]
[Authorize(Roles = Roles.Nutritionist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistQuestionnairesController(
    ManageQuestionnaireUseCase manageUseCase,
    IAnamnesisQueries anamnesisQueries) : ControllerBase
{
    /// <summary>Lista os questionários do nutricionista, em ordem alfabética.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<QuestionnaireSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> List([FromQuery] AnamnesisListRequest request, CancellationToken cancellationToken) =>
        Ok(await anamnesisQueries.ListQuestionnairesAsync(User.GetUserId(), request, cancellationToken));

    /// <summary>Devolve o modelo base, no formato do corpo do POST, para o nutricionista editar e criar o seu.</summary>
    [HttpGet("template")]
    [ProducesResponseType<QuestionnaireRequest>(StatusCodes.Status200OK)]
    public IActionResult GetTemplate() => Ok(QuestionnaireTemplate.Base);

    /// <summary>Devolve um questionário, com as perguntas.</summary>
    /// <param name="id">O Id do questionário.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<QuestionnaireResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var questionnaire = await anamnesisQueries.GetQuestionnaireAsync(User.GetUserId(), id, cancellationToken);

        return questionnaire is null ? QuestionnaireNotFound() : Ok(questionnaire);
    }

    /// <summary>Cria um questionário.</summary>
    [HttpPost]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(QuestionnaireRequest request, CancellationToken cancellationToken)
    {
        var id = await manageUseCase.CreateAsync(User.GetUserId(), request, cancellationToken);

        return Created($"/api/v1/nutritionists/me/questionnaires/{id}", new IdResponse(id));
    }

    /// <summary>Troca o título e as perguntas do questionário.</summary>
    /// <remarks>As perguntas são substituídas por inteiro e ganham Ids novos. As anamneses já enviadas não mudam.</remarks>
    /// <param name="id">O Id do questionário.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, QuestionnaireRequest request, CancellationToken cancellationToken) =>
        await manageUseCase.UpdateAsync(User.GetUserId(), id, request, cancellationToken) ? NoContent() : QuestionnaireNotFound();

    /// <summary>Apaga o questionário. As anamneses já enviadas com ele continuam.</summary>
    /// <param name="id">O Id do questionário.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await manageUseCase.DeleteAsync(User.GetUserId(), id, cancellationToken) ? NoContent() : QuestionnaireNotFound();

    private ObjectResult QuestionnaireNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Questionário não encontrado.");
}
