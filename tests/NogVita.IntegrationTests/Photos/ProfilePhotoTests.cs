using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NogVita.IntegrationTests.Infrastructure;
using SkiaSharp;

namespace NogVita.IntegrationTests.Photos;

public class ProfilePhotoTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Followed_patient_photo_is_visible_to_nutritionist()
    {
        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);

        var upload = await _accounts.As(patient).PutAsync("/api/v1/patients/me/photo", PhotoForm(), _ct);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var page = await _accounts.As(nutritionist)
            .GetFromJsonAsync<JsonElement>("/api/v1/nutritionists/me/patients", TestAccounts.Json, _ct);

        var item = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.StartsWith("data:image/webp;base64,", item.GetProperty("photoUrl").GetString());
    }

    [Fact]
    public async Task Patient_without_photo_gets_null_url()
    {
        var patient = await _accounts.NewPatientAsync();

        var me = await _accounts.As(patient).GetFromJsonAsync<JsonElement>("/api/v1/patients/me", TestAccounts.Json, _ct);

        Assert.Equal(JsonValueKind.Null, me.GetProperty("photoUrl").ValueKind);
    }

    [Fact]
    public async Task File_that_is_not_an_image_is_400()
    {
        var patient = await _accounts.NewPatientAsync();
        var form = new MultipartFormDataContent { { new ByteArrayContent("MZ não é imagem"u8.ToArray()), "file", "foto.jpg" } };

        var upload = await _accounts.As(patient).PutAsync("/api/v1/patients/me/photo", form, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    private static MultipartFormDataContent PhotoForm()
    {
        using var bitmap = new SKBitmap(800, 600);
        bitmap.Erase(SKColors.SeaGreen);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);

        var file = new ByteArrayContent(data.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return new MultipartFormDataContent { { file, "file", "foto.jpg" } };
    }
}
