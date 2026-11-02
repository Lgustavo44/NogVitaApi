using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;

namespace NogVita.Application.Photos;

public static class FileStorageCleanup
{
    // A limpeza depois de o banco confirmar. Uma falha aqui só deixa um arquivo órfão,
    // e nenhum registro aponta mais para ele: vira um aviso no log, e não um erro para o usuário.
    public static async Task TryDeleteAsync(this IFileStorage storage, StorageBucket bucket, IReadOnlyCollection<string> paths, ILogger logger)
    {
        if (paths.Count == 0)
        {
            return;
        }

        try
        {
            // CancellationToken.None: a limpeza termina mesmo se o usuário fechar a página no meio.
            await storage.DeleteAsync(bucket, paths, CancellationToken.None);
        }
        catch (Exception exception)
        {
            // D40: os caminhos só têm IDs.
            logger.LogWarning(exception, "Não foi possível apagar {Count} arquivo(s) do bucket {Bucket}: {Paths}", paths.Count, bucket, paths);
        }
    }
}
