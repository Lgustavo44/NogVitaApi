using System.Net;
using System.Text;
using NogVita.Application.Abstractions;
using NogVita.Infrastructure.Storage;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Infrastructure.Storage;

public class SupabaseFileStorageTests
{
    private static readonly StorageSettings Settings = new()
    {
        Provider = "Supabase",
        Url = "https://projeto.supabase.co",
        SecretKey = "sb_secret_teste"
    };

    [Fact]
    public async Task Upload_posts_to_bucket_path_replacing_existing_file()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await CreateStorage(handler).UploadAsync(StorageBucket.PatientPhotos, "p1/profile/f1.webp", [1, 2, 3], "image/webp", TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://projeto.supabase.co/storage/v1/object/patient-photos/p1/profile/f1.webp", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("true", Assert.Single(handler.LastRequest.Headers.GetValues("x-upsert")));
    }

    [Fact]
    public async Task Signed_urls_are_absolute_and_skip_missing_files()
    {
        const string json = """
            [
              { "path": "p1/profile/f1.webp", "signedURL": "/object/sign/patient-photos/p1/profile/f1.webp?token=abc", "error": null },
              { "path": "p1/profile/sumiu.webp", "signedURL": null, "error": "Either the object does not exist or you do not have access to it" }
            ]
            """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var urls = await CreateStorage(handler).CreateSignedUrlsAsync(
            StorageBucket.PatientPhotos, ["p1/profile/f1.webp", "p1/profile/sumiu.webp"], TestContext.Current.CancellationToken);

        var url = Assert.Single(urls);
        Assert.Equal("p1/profile/f1.webp", url.Key);
        Assert.Equal("https://projeto.supabase.co/storage/v1/object/sign/patient-photos/p1/profile/f1.webp?token=abc", url.Value);
    }

    [Fact]
    public async Task Rejected_request_throws_with_status()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateStorage(handler).UploadAsync(StorageBucket.PatientPhotos, "p1/x.webp", [1], "image/webp", TestContext.Current.CancellationToken));

        Assert.Contains("403", exception.Message);
    }

    [Fact]
    public void Public_url_is_refused_for_private_bucket()
    {
        var storage = CreateStorage(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.Throws<InvalidOperationException>(() => storage.GetPublicUrl(StorageBucket.PatientPhotos, "p1/x.webp"));
    }

    [Fact]
    public void Supabase_provider_requires_secret_key()
    {
        var settings = new StorageSettings { Provider = "Supabase", Url = "https://projeto.supabase.co" };

        Assert.Throws<InvalidOperationException>(settings.Validate);
    }

    private static SupabaseFileStorage CreateStorage(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://projeto.supabase.co/") }, Settings);
}
