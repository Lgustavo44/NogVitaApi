using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Application.Foods;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/foods")]
[Authorize(Roles = $"{Roles.Nutritionist},{Roles.Admin}")]
public sealed class FoodsController(
    LookupProductByBarcodeUseCase lookupProductUseCase,
    ImportProductUseCase importProductUseCase,
    IFoodQueries foodQueries) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] FoodSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await foodQueries.SearchAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var food = await foodQueries.GetByIdAsync(id, includeInactive: User.IsInRole(Roles.Admin), cancellationToken);

        return food is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Alimento não encontrado.")
            : Ok(food);
    }

    [HttpGet("barcode/{barcode}")]
    [EnableRateLimiting("food-lookup")]
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
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "O catálogo de produtos está indisponível no momento. Tente novamente em alguns minutos.")
        };
    }

    [HttpPost("barcode/{barcode}/import")]
    [EnableRateLimiting("food-lookup")]
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
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "O catálogo de produtos está indisponível no momento. Tente novamente em alguns minutos.")
        };
    }
}