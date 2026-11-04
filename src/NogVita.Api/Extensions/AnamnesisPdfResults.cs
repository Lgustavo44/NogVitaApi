using Microsoft.AspNetCore.Mvc;
using NogVita.Application.Anamneses;

namespace NogVita.Api.Extensions;

public static class AnamnesisPdfResults
{
    public static IActionResult ToActionResult(this ControllerBase controller, AnamnesisPdfResult result) => result.Status switch
    {
        AnamnesisPdfStatus.Done => controller.File(result.File!.Content, "application/pdf", result.File.FileName),
        AnamnesisPdfStatus.NotSubmitted => controller.Problem(
            statusCode: StatusCodes.Status409Conflict, title: "A anamnese ainda não foi respondida."),
        _ => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: "Anamnese não encontrada.")
    };
}
