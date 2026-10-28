using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NogVita.Api.OpenApi;

// O título e a descrição do documento: o que alguém que nunca viu o NogVita precisa saber antes do primeiro endpoint.
internal sealed class DocumentInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "NogVita API",
            Version = "v1",
            Description = """
                API do NogVita, uma plataforma de acompanhamento nutricional entre pacientes, nutricionistas e administradores.

                **Autenticação.** Entre com `POST /api/v1/auth/login` e envie o `accessToken` no header `Authorization: Bearer <token>`. O token de acesso dura 15 minutos; renove com `POST /api/v1/auth/refresh`, uma renovação por vez. No Swagger, use o botão **Authorize**.

                **Papéis.** `Patient`, `Nutritionist` e `Admin`. Um usuário pode ter mais de um; `GET /api/v1/auth/me` diz quais.

                **Erros.** Toda resposta de erro com corpo segue o ProblemDetails (RFC 9457), com as mensagens em português, prontas para o usuário. O `400` de validação traz `errors`, campo por campo; quando o `400` indica uma regra de negócio, o motivo vem em `title`. O `401` por falta de token e o `403` por falta de papel vêm sem corpo. Um recurso de outro usuário responde `404`, e não `403`. Um `500` nunca traz detalhes internos, só o `traceId`.

                **Formatos.** Datas em `YYYY-MM-DD`, horários em `HH:mm`, instantes em UTC, enums como texto (`"MuscleGain"`).

                **Versão.** Esta é a `v1`, no caminho de cada rota (`/api/v1/...`). Veja a [política de versionamento](https://github.com/Lgustavo44/NogVitaApi/blob/main/docs/dev/versionamento.md).
                """
        };

        return Task.CompletedTask;
    }
}
