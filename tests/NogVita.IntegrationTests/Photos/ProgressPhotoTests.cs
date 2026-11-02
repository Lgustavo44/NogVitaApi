using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NogVita.IntegrationTests.Infrastructure;
using SkiaSharp;

namespace NogVita.IntegrationTests.Photos;

public class ProgressPhotoTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Patient_consents_uploads_and_followed_nutritionist_sees_it()
    {
        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);
        var client = _accounts.As(patient);

        var consent = await client.GetFromJsonAsync<JsonElement>("/api/v1/patients/me/progress-photos/consent", TestAccounts.Json, _ct);
        var accept = await client.PostAsJsonAsync(
            "/api/v1/patients/me/progress-photos/consent", new { version = consent.GetProperty("version").GetString() }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);

        // Ontem, para não depender do fuso: hoje em UTC pode ser "amanhã" no horário de Brasília.
        var takenOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var form = PhotoForm();
        form.Add(new StringContent(takenOn.ToString("yyyy-MM-dd")), "takenOn");
        form.Add(new StringContent("Front"), "angle");

        var upload = await client.PostAsync("/api/v1/patients/me/progress-photos", form, _ct);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var days = await _accounts.As(nutritionist).GetFromJsonAsync<JsonElement>(
            $"/api/v1/nutritionists/me/patients/{patient.Id}/progress-photos", TestAccounts.Json, _ct);

        var day = Assert.Single(days.EnumerateArray());
        var photo = Assert.Single(day.GetProperty("photos").EnumerateArray());
        Assert.Equal("Front", photo.GetProperty("angle").GetString());
    }

    [Fact]
    public async Task Upload_without_consent_is_422()
    {
        var patient = await _accounts.NewPatientAsync();
        var form = PhotoForm();
        form.Add(new StringContent(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd")), "takenOn");
        form.Add(new StringContent("Side"), "angle");

        var upload = await _accounts.As(patient).PostAsync("/api/v1/patients/me/progress-photos", form, _ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, upload.StatusCode);
    }

    // O mesmo da Etapa 4 (ProfilePhotoTests).
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
