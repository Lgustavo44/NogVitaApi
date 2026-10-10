using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Application.Foods;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/foods")]
[Authorize(Roles = $"{Roles.Nutritionist},{Roles.Admin}")]
public sealed class FoodsController(LookupProductByBarcodeUseCase lookupProductUseCase) : ControllerBase
{
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
}