using Microsoft.AspNetCore.Mvc;
using NogVita.Application.Photos;

namespace NogVita.Api.Extensions;

public static class ProfilePhotoResults
{
    // D-V2-07: o arquivo enviado. Acima disso, o próprio ASP.NET responde 413 antes de ler o corpo.
    public const long MaxUploadBytes = 5 * 1024 * 1024;

    public static IActionResult ToActionResult(this ControllerBase controller, ProfilePhotoOutcome outcome) => outcome.Result switch
    {
        ProfilePhotoResult.Done => outcome.PhotoUrl is null
            ? controller.NoContent()
            : controller.Ok(new ProfilePhotoResponse(outcome.PhotoUrl)),
        ProfilePhotoResult.ProfileNotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil não encontrado."),
        ProfilePhotoResult.InvalidImage => controller.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Envie uma imagem JPEG, PNG ou WebP."),
        ProfilePhotoResult.ImageTooLarge => controller.Problem(statusCode: StatusCodes.Status400BadRequest, title: "A imagem tem resolução grande demais. Envie uma foto de até 40 megapixels.")
    };
}
