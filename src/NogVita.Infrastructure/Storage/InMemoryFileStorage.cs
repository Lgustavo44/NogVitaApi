using System.Collections.Concurrent;
using NogVita.Application.Abstractions;

namespace NogVita.Infrastructure.Storage;

// D-V2-13: desenvolvimento e testes. As imagens voltam como "data:" URL, que o <img> do navegador exibe direto.
// Tudo some quando a API reinicia. Público para os testes de integração conferirem o que foi gravado.
public sealed class InMemoryFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<(StorageBucket Bucket, string Path), StoredFile> _files = new();

    public bool Contains(StorageBucket bucket, string path) => _files.ContainsKey((bucket, path));

    public Task UploadAsync(StorageBucket bucket, string path, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        _files[(bucket, path)] = new StoredFile(content, contentType);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StorageBucket bucket, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default)
    {
        foreach (var path in paths)
        {
            _files.TryRemove((bucket, path), out _);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, string>> CreateSignedUrlsAsync(StorageBucket bucket, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, string> urls = paths
            .Distinct()
            .Where(path => _files.ContainsKey((bucket, path)))
            .ToDictionary(path => path, path => _files[(bucket, path)].ToDataUrl());

        return Task.FromResult(urls);
    }

    public string GetPublicUrl(StorageBucket bucket, string path) => bucket switch
    {
        StorageBucket.NutritionistPhotos => _files.TryGetValue((bucket, path), out var file) ? file.ToDataUrl() : string.Empty,
        StorageBucket.PatientPhotos => throw new InvalidOperationException("O bucket das fotos do paciente é privado: use CreateSignedUrlsAsync.")
    };

    private sealed record StoredFile(byte[] Content, string ContentType)
    {
        public string ToDataUrl() => $"data:{ContentType};base64,{Convert.ToBase64String(Content)}";
    }
}
