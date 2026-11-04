using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.OpenApi;
using NogVita.Application.Documents;

namespace NogVita.Api.Controllers;

/// <summary>A conferência pública dos PDFs (planos alimentares e anamneses), pelo código verificador.</summary>
/// <remarks>A rota continua `plan-documents` por compatibilidade com o front; `kind` diz o tipo do documento.</remarks>
[ApiController]
[Route("api/v1/plan-documents")]
[AllowAnonymous]
[EnableRateLimiting("auth")] // o mesmo limite por IP das rotas públicas: dificulta tentar códigos em sequência
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class IssuedDocumentsController(DocumentIssuer documentIssuer) : ControllerBase
{
    /// <summary>Confere um PDF de plano alimentar ou de anamnese pelo código impresso no rodapé.</summary>
    /// <remarks>
    /// Devolve o tipo do documento (`NutritionPlan` ou `Anamnesis`), quem o emitiu (o nome e o CRN do nutricionista), quando, e o SHA-256 do arquivo emitido,
    /// para comparar com o arquivo recebido. Não traz nenhum dado do paciente. O código vale com ou sem os traços.
    /// </remarks>
    /// <param name="code">O código verificador, por exemplo `ABCD-EFGH-JKLM`.</param>
    [HttpGet("{code}")]
    [ProducesResponseType<DocumentVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify(string code, CancellationToken cancellationToken)
    {
        var document = await documentIssuer.VerifyAsync(code, cancellationToken);

        return document is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Documento não encontrado. Confira o código.")
            : Ok(document);
    }
}
