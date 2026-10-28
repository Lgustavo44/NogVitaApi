using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.OpenApi;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Foods;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>A base de alimentos: a TACO e os produtos do Open Food Facts.</summary>
/// <remarks>
/// Toda resposta com dados de alimentos traz o campo `attribution` (a fonte e a licença), que precisa ser exibido.
/// Os alimentos desativados pelo administrador não aparecem aqui.
/// </remarks>
[ApiController]
[Route("api/v1/foods")]
[Authorize(Roles = $"{Roles.Nutritionist},{Roles.Admin}")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class FoodsController(
    LookupProductByBarcodeUseCase lookupProductUseCase,
    ImportProductUseCase importProductUseCase,
    IFoodQueries foodQueries) : ControllerBase
{
    /// <summary>Busca alimentos pelo nome.</summary>
    /// <remarks>
    /// Cada palavra precisa aparecer no nome, em qualquer ordem, sem diferenciar acentos e maiúsculas ("acucar mascavo"
    /// acha "Açúcar, mascavo"). Pelo menos 2 caracteres. `source` filtra pela fonte: `Taco` ou `OpenFoodFacts`.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<PagedResponse<FoodResponse>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> Search([FromQuery] FoodSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await foodQueries.SearchAsync(request, cancellationToken: cancellationToken);

        return Ok(response);
    }

    /// <summary>Devolve um alimento, com os valores por 100 g.</summary>
    /// <remarks>Para o administrador, devolve também os alimentos desativados.</remarks>
    /// <param name="id">O Id do alimento.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<FoodResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var food = await foodQueries.GetByIdAsync(id, includeInactive: User.IsInRole(Roles.Admin), cancellationToken);

        return food is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Alimento não encontrado.")
            : Ok(food);
    }

    /// <summary>Mostra a prévia de um produto pelo código de barras, sem importar.</summary>
    /// <remarks>
    /// Procura primeiro na base local; se não achar, consulta o Open Food Facts. Códigos de 8, 12, 13 ou 14 dígitos.
    /// No máximo 10 consultas por minuto. Se o Open Food Facts estiver fora do ar, a resposta é 503.
    /// </remarks>
    /// <param name="barcode">O código de barras (EAN-8, UPC-A, EAN-13 ou GTIN-14).</param>
    [HttpGet("barcode/{barcode}")]
    [EnableRateLimiting("food-lookup")]
    [ProducesResponseType<ProductPreviewResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesProblem(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        var result = await lookupProductUseCase.ExecuteAsync(barcode, cancellationToken);

        return result.Status switch
        {
            LookupProductStatus.Found => Ok(result.Preview),
            LookupProductStatus.InvalidBarcode => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Código de barras inválido. Use 8, 12, 13 ou 14 dígitos."),
            LookupProductStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Produto não encontrado no Open Food Facts."),
            LookupProductStatus.Unavailable => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "O catálogo de produtos está indisponível no momento. Tente novamente em alguns minutos.")
        };
    }

    /// <summary>Importa um produto do Open Food Facts para a base de alimentos.</summary>
    /// <remarks>
    /// 201 quando importa; 200 quando o produto já estava na base (inclusive desativado, que continua desativado).
    /// Um produto com valores nutricionais impossíveis é recusado (422). No máximo 10 importações por minuto.
    /// </remarks>
    /// <param name="barcode">O código de barras (EAN-8, UPC-A, EAN-13 ou GTIN-14).</param>
    [HttpPost("barcode/{barcode}/import")]
    [EnableRateLimiting("food-lookup")]
    [ProducesResponseType<FoodResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<FoodResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesProblem(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Import(string barcode, CancellationToken cancellationToken)
    {
        var result = await importProductUseCase.ExecuteAsync(barcode, cancellationToken);

        return result.Status switch
        {
            ImportProductStatus.Imported => Created($"/api/v1/foods/{result.Food!.Id}", result.Food),
            ImportProductStatus.AlreadyImported => Ok(result.Food),
            ImportProductStatus.InvalidBarcode => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Código de barras inválido. Use 8, 12, 13 ou 14 dígitos."),
            ImportProductStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Produto não encontrado."),
            ImportProductStatus.InvalidData => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Os dados nutricionais deste produto no Open Food Facts são inconsistentes e não podem ser importados."),
            ImportProductStatus.Unavailable => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "O catálogo de produtos está indisponível no momento. Tente novamente em alguns minutos.")
        };
    }
}
