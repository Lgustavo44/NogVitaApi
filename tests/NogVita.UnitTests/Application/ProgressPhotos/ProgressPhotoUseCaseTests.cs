using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Abstractions;
using NogVita.Application.FollowUps;
using NogVita.Application.ProgressPhotos;
using NogVita.Domain.ProgressPhotos;
using NogVita.Domain.Users;
using NogVita.Infrastructure.Images;
using NogVita.Infrastructure.Storage;
using NogVita.UnitTests.Fakes;
using SkiaSharp;

namespace NogVita.UnitTests.Application.ProgressPhotos;

public class ProgressPhotoUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 2);

    private readonly FakeUserRepository _users = new();
    private readonly FakeProgressPhotoRepository _photos = new();
    private readonly FakeCareRelationshipRepository _relationships = new();
    private readonly InMemoryFileStorage _storage = new();
    private readonly ProgressPhotoUseCase _useCase;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public ProgressPhotoUseCaseTests() =>
        _useCase = new ProgressPhotoUseCase(
            _users, _photos, new FakeUnitOfWork(), new SkiaImageProcessor(), _storage,
            new CareAccessService(_users, _relationships), new FixedTimeProvider(Now), NullLogger<ProgressPhotoUseCase>.Instance);

    [Fact]
    public async Task Upload_without_consent_is_refused()
    {
        var patient = TestUsers.Patient().AddTo(_users);

        var outcome = await _useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Front, CreateImage(), _ct);

        Assert.Equal(ProgressPhotoResult.ConsentRequired, outcome.Result);
        Assert.Empty(_photos.Photos);
    }

    [Fact]
    public async Task Outdated_consent_version_is_refused()
    {
        var patient = TestUsers.Patient().AddTo(_users);

        var result = await _useCase.AcceptConsentAsync(patient.Id, "0", _ct);

        Assert.Equal(ProgressPhotoResult.OutdatedConsentVersion, result);
    }

    [Fact]
    public async Task Same_date_and_angle_replaces_file()
    {
        var patient = ConsentingPatient();
        await _useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Front, CreateImage(), _ct);
        var firstPath = _photos.Photos.Single().StoragePath;

        await _useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Front, CreateImage(), _ct);

        var photo = Assert.Single(_photos.Photos);
        Assert.StartsWith($"{patient.Id}/progress/", photo.StoragePath);
        Assert.NotEqual(firstPath, photo.StoragePath);
        Assert.False(_storage.Contains(StorageBucket.PatientPhotos, firstPath));
    }

    [Fact]
    public async Task Limit_blocks_a_new_photo()
    {
        var patient = ConsentingPatient();
        for (var day = 1; day <= ProgressPhoto.MaxPerPatient / 3; day++)
        {
            foreach (var angle in Enum.GetValues<ProgressPhotoAngle>())
            {
                _photos.Add(new ProgressPhoto(patient.Id, Today.AddDays(-day), angle, $"{patient.Id}/progress/{day}-{angle}.webp", Today));
            }
        }

        var outcome = await _useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Front, CreateImage(), _ct);

        Assert.Equal(ProgressPhotoResult.LimitReached, outcome.Result);
    }

    [Fact]
    public async Task Revoking_consent_deletes_every_photo()
    {
        var patient = ConsentingPatient();
        await _useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Front, CreateImage(), _ct);
        await _useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Side, CreateImage(), _ct);
        var paths = _photos.Photos.Select(p => p.StoragePath).ToList();

        var result = await _useCase.RevokeConsentAsync(patient.Id, _ct);

        Assert.Equal(ProgressPhotoResult.Done, result);
        Assert.Empty(_photos.Photos);
        Assert.All(paths, path => Assert.False(_storage.Contains(StorageBucket.PatientPhotos, path)));
        Assert.False(patient.PatientProfile!.HasProgressPhotosConsent(ProgressPhotoConsent.CurrentVersion));
    }

    [Fact]
    public async Task Patient_cannot_delete_someone_elses_photo()
    {
        var owner = ConsentingPatient();
        var other = TestUsers.Patient(email: "outro@email.com").AddTo(_users);
        await _useCase.UploadAsync(owner.Id, Today, ProgressPhotoAngle.Front, CreateImage(), _ct);

        var result = await _useCase.DeleteAsync(other.Id, _photos.Photos.Single().Id, _ct);

        Assert.Equal(ProgressPhotoResult.NotFound, result);
        Assert.Single(_photos.Photos);
    }

    [Fact]
    public async Task Nutritionist_without_relationship_sees_nothing()
    {
        var patient = ConsentingPatient();
        var nutritionist = TestUsers.Nutritionist().AddTo(_users);

        var outcome = await _useCase.ListForNutritionistAsync(nutritionist.Id, patient.Id, _ct);

        Assert.Equal(ProgressPhotoResult.NotFound, outcome.Result);
    }

    [Fact]
    public async Task Following_nutritionist_sees_photos_ordered_by_angle()
    {
        var patient = ConsentingPatient();
        var nutritionist = TestUsers.Nutritionist().AddTo(_users);
        _relationships.Add(TestUsers.Follow(patient.Id, nutritionist.Id));
        await _useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Back, CreateImage(), _ct);
        await _useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Front, CreateImage(), _ct);

        var outcome = await _useCase.ListForNutritionistAsync(nutritionist.Id, patient.Id, _ct);

        var day = Assert.Single(outcome.Value!);
        Assert.Equal(new[] { ProgressPhotoAngle.Front, ProgressPhotoAngle.Back }, day.Photos.Select(p => p.Angle));
        Assert.All(day.Photos, p => Assert.StartsWith("data:image/webp", p.Url));
    }

    [Fact]
    public async Task Unknown_patient_is_profile_not_found()
    {
        var outcome = await _useCase.GetConsentAsync(Guid.NewGuid(), _ct);

        Assert.Equal(ProgressPhotoResult.ProfileNotFound, outcome.Result);
    }

    [Fact]
    public async Task File_that_is_not_an_image_stores_nothing()
    {
        var patient = ConsentingPatient();

        var outcome = await _useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Front, new MemoryStream("não é imagem"u8.ToArray()), _ct);

        Assert.Equal(ProgressPhotoResult.InvalidImage, outcome.Result);
        Assert.Empty(_photos.Photos);
    }

    [Fact]
    public async Task Photo_above_pixel_limit_stores_nothing()
    {
        // O processador de verdade, com um limite abaixo dos 800 x 600 da imagem de teste (480 mil pixels).
        var useCase = new ProgressPhotoUseCase(
            _users, _photos, new FakeUnitOfWork(), new SkiaImageProcessor(maxPixels: 100_000), _storage,
            new CareAccessService(_users, _relationships), new FixedTimeProvider(Now), NullLogger<ProgressPhotoUseCase>.Instance);
        var patient = ConsentingPatient();

        var outcome = await useCase.UploadAsync(patient.Id, Today, ProgressPhotoAngle.Front, CreateImage(), _ct);

        Assert.Equal(ProgressPhotoResult.ImageTooLarge, outcome.Result);
        Assert.Empty(_photos.Photos);
    }

    private User ConsentingPatient()
    {
        var patient = TestUsers.Patient().AddTo(_users);
        patient.GrantProgressPhotosConsent(ProgressPhotoConsent.CurrentVersion, Now.UtcDateTime);
        return patient;
    }

    private static MemoryStream CreateImage()
    {
        using var bitmap = new SKBitmap(800, 600);
        bitmap.Erase(SKColors.SlateBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return new MemoryStream(data.ToArray());
    }
}
