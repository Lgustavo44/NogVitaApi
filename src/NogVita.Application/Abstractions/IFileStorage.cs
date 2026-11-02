namespace NogVita.Application.Abstractions;

public enum StorageBucket
{
    NutritionistPhotos, // público (D-V2-03)
    PatientPhotos       // privado: só por URL assinada (D-V2-11, D-V2-12)
}

public interface IFileStorage
{
    // Grava o arquivo. Se o caminho já existir, substitui (reenviar o mesmo ângulo, D-V2-05).
    Task UploadAsync(StorageBucket bucket, string path, byte[] content, string contentType, CancellationToken cancellationToken = default);

    Task DeleteAsync(StorageBucket bucket, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default);

    // Uma chamada só para a lista inteira. O dicionário vai do caminho para a URL temporária.
    Task<IReadOnlyDictionary<string, string>> CreateSignedUrlsAsync(StorageBucket bucket, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default);

    // Só para o bucket público.
    string GetPublicUrl(StorageBucket bucket, string path);
}
