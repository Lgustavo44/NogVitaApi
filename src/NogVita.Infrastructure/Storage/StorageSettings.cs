using NogVita.Application.Abstractions;

namespace NogVita.Infrastructure.Storage;

public sealed class StorageSettings
{
    public const string SectionName = "Storage";

    public string Provider { get; init; } = "InMemory";
    public string? Url { get; init; }
    public string? SecretKey { get; init; }
    public string NutritionistPhotosBucket { get; init; } = "nutritionist-photos";
    public string PatientPhotosBucket { get; init; } = "patient-photos";
    public int SignedUrlLifetimeMinutes { get; init; } = 5;

    public bool UsesSupabase => Provider.Equals("Supabase", StringComparison.OrdinalIgnoreCase);

    public string BucketName(StorageBucket bucket) => bucket switch
    {
        StorageBucket.NutritionistPhotos => NutritionistPhotosBucket,
        StorageBucket.PatientPhotos => PatientPhotosBucket
    };

    public void Validate()
    {
        if (!UsesSupabase && !Provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage: Provider precisa ser Supabase ou InMemory.");
        }

        if (SignedUrlLifetimeMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException("Storage: SignedUrlLifetimeMinutes precisa ficar entre 1 e 60.");
        }

        if (!UsesSupabase)
        {
            return;
        }

        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Storage: Url precisa ser uma URL https absoluta para o provedor Supabase.");
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException("Storage: SecretKey é obrigatória para o provedor Supabase.");
        }
    }
}
