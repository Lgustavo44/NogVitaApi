using NogVita.Application.Abstractions;

namespace NogVita.Application.Photos;

public static class PhotoUrls
{
    // O bucket do nutricionista é público: a URL é montada sem chamar o Supabase.
    public static string? NutritionistPhotoUrl(this IFileStorage storage, string? path) =>
        path is null ? null : storage.GetPublicUrl(StorageBucket.NutritionistPhotos, path);

    // Uma chamada só para a página inteira: uma lista de 20 pacientes não vira 20 chamadas ao Supabase.
    public static async Task<IReadOnlyDictionary<string, string>> SignPatientPhotosAsync(
        this IFileStorage storage, IEnumerable<string?> paths, CancellationToken cancellationToken = default)
    {
        var distinctPaths = paths.OfType<string>().Distinct().ToList();

        return distinctPaths.Count == 0
            ? new Dictionary<string, string>()
            : await storage.CreateSignedUrlsAsync(StorageBucket.PatientPhotos, distinctPaths, cancellationToken);
    }

    public static async Task<string?> PatientPhotoUrlAsync(this IFileStorage storage, string? path, CancellationToken cancellationToken = default) =>
        (await storage.SignPatientPhotosAsync([path], cancellationToken)).UrlFor(path);

    // Sem foto, ou com um arquivo que sumiu do bucket: null, e o front mostra o avatar padrão.
    public static string? UrlFor(this IReadOnlyDictionary<string, string> urls, string? path) =>
        path is not null && urls.TryGetValue(path, out var url) ? url : null;
}
