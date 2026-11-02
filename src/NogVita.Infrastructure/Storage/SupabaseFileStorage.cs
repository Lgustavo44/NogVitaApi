using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NogVita.Application.Abstractions;

namespace NogVita.Infrastructure.Storage;

// D-V2-09: a API REST do Supabase Storage, por um HttpClient próprio (o mesmo padrão do Brevo).
internal sealed class SupabaseFileStorage(HttpClient httpClient, StorageSettings settings) : IFileStorage
{
    public async Task UploadAsync(StorageBucket bucket, string path, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{settings.BucketName(bucket)}/{path}")
        {
            Content = new ByteArrayContent(content)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Headers.Add("x-upsert", "true");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "o envio do arquivo", cancellationToken);
    }

    public async Task DeleteAsync(StorageBucket bucket, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default)
    {
        if (paths.Count == 0)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"storage/v1/object/{settings.BucketName(bucket)}")
        {
            Content = JsonContent.Create(new { prefixes = paths })
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "a remoção dos arquivos", cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> CreateSignedUrlsAsync(StorageBucket bucket, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default)
    {
        if (paths.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var payload = new { expiresIn = settings.SignedUrlLifetimeMinutes * 60, paths };

        using var response = await httpClient.PostAsJsonAsync($"storage/v1/object/sign/{settings.BucketName(bucket)}", payload, cancellationToken);
        await EnsureSuccessAsync(response, "a geração dos links temporários", cancellationToken);

        var signed = await response.Content.ReadFromJsonAsync<List<SignedUrlResponse>>(cancellationToken) ?? [];

        // O signedURL vem relativo a /storage/v1 ("/object/sign/patient-photos/...?token=...").
        // Um caminho que não existe volta com "error", e fica fora do dicionário: o front mostra o avatar padrão.
        return signed
            .Where(s => s.Error is null && s.SignedUrl is not null)
            .ToDictionary(s => s.Path, s => $"{BaseUrl}/storage/v1{s.SignedUrl}");
    }

    public string GetPublicUrl(StorageBucket bucket, string path) => bucket switch
    {
        StorageBucket.NutritionistPhotos => $"{BaseUrl}/storage/v1/object/public/{settings.BucketName(bucket)}/{path}",
        StorageBucket.PatientPhotos => throw new InvalidOperationException("O bucket das fotos do paciente é privado: use CreateSignedUrlsAsync.")
    };

    private string BaseUrl => settings.Url!.TrimEnd('/');

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string action, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Vira um 500 genérico com traceId (D167). O detalhe fica só no log do servidor.
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"O Supabase Storage recusou {action} ({(int)response.StatusCode}): {body}");
    }

    private sealed record SignedUrlResponse(
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("signedURL")] string? SignedUrl,
        [property: JsonPropertyName("error")] string? Error);
}
