using Microsoft.AspNetCore.Mvc;
using NogVita.Application.ProgressPhotos;

namespace NogVita.Api.Extensions;

public static class ProgressPhotoResults
{
    public static IActionResult ToActionResult(this ControllerBase controller, ProgressPhotoResult result) => result switch
    {
        ProgressPhotoResult.Done => controller.NoContent(),
        ProgressPhotoResult.ProfileNotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado."),
        ProgressPhotoResult.NotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: "Não encontrado."),
        ProgressPhotoResult.ConsentRequired => controller.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Aceite o termo das fotos de evolução antes de enviar."),
        ProgressPhotoResult.OutdatedConsentVersion => controller.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "O termo foi atualizado. Leia a versão nova e aceite de novo."),
        ProgressPhotoResult.LimitReached => controller.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Você chegou ao limite de 90 fotos. Apague fotos antigas para enviar novas."),
        ProgressPhotoResult.InvalidImage => controller.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Envie uma imagem JPEG, PNG ou WebP."),
        ProgressPhotoResult.ImageTooLarge => controller.Problem(statusCode: StatusCodes.Status400BadRequest, title: "A imagem tem resolução grande demais. Envie uma foto de até 40 megapixels.")
    };

    public static IActionResult ToActionResult<T>(this ControllerBase controller, ProgressPhotoOutcome<T> outcome) =>
        outcome.Result == ProgressPhotoResult.Done ? controller.Ok(outcome.Value) : controller.ToActionResult(outcome.Result);
}
