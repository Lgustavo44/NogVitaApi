using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.Abstractions;
using NogVita.Application.Photos;
using NogVita.Infrastructure.Images;
using NogVita.Infrastructure.Storage;
using NogVita.UnitTests.Fakes;
using SkiaSharp;

namespace NogVita.UnitTests.Application.Photos;

public class ProfilePhotoUseCaseTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly InMemoryFileStorage _storage = new();
    private readonly ProfilePhotoUseCase _useCase;

    public ProfilePhotoUseCaseTests() =>
        _useCase = new ProfilePhotoUseCase(_users, _unitOfWork, new SkiaImageProcessor(), _storage, NullLogger<ProfilePhotoUseCase>.Instance);

    [Fact]
    public async Task Patient_photo_goes_to_private_bucket_under_own_folder()
    {
        var patient = TestUsers.Patient().AddTo(_users);

        var outcome = await _useCase.UploadAsync(patient.Id, ProfilePhotoOwner.Patient, CreateImage(), TestContext.Current.CancellationToken);

        Assert.Equal(ProfilePhotoResult.Done, outcome.Result);
        Assert.StartsWith("data:image/webp;base64,", outcome.PhotoUrl);
        Assert.StartsWith($"{patient.Id}/profile/", patient.PatientProfile!.PhotoPath);
        Assert.True(_storage.Contains(StorageBucket.PatientPhotos, patient.PatientProfile.PhotoPath!));
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task New_photo_replaces_and_deletes_previous_file()
    {
        var nutritionist = TestUsers.Nutritionist().AddTo(_users);
        await _useCase.UploadAsync(nutritionist.Id, ProfilePhotoOwner.Nutritionist, CreateImage(), TestContext.Current.CancellationToken);
        var firstPath = nutritionist.NutritionistProfile!.PhotoPath!;

        await _useCase.UploadAsync(nutritionist.Id, ProfilePhotoOwner.Nutritionist, CreateImage(), TestContext.Current.CancellationToken);

        Assert.NotEqual(firstPath, nutritionist.NutritionistProfile.PhotoPath);
        Assert.False(_storage.Contains(StorageBucket.NutritionistPhotos, firstPath));
        Assert.True(_storage.Contains(StorageBucket.NutritionistPhotos, nutritionist.NutritionistProfile.PhotoPath!));
    }

    [Fact]
    public async Task Invalid_image_stores_nothing()
    {
        var patient = TestUsers.Patient().AddTo(_users);

        var outcome = await _useCase.UploadAsync(patient.Id, ProfilePhotoOwner.Patient, new MemoryStream("não é imagem"u8.ToArray()), TestContext.Current.CancellationToken);

        Assert.Equal(ProfilePhotoResult.InvalidImage, outcome.Result);
        Assert.Null(patient.PatientProfile!.PhotoPath);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Photo_above_pixel_limit_stores_nothing()
    {
        // O processador de verdade, com um limite abaixo dos 1200 x 900 da imagem de teste (1,08 milhão de pixels).
        var useCase = new ProfilePhotoUseCase(_users, _unitOfWork, new SkiaImageProcessor(maxPixels: 100_000), _storage, NullLogger<ProfilePhotoUseCase>.Instance);
        var patient = TestUsers.Patient().AddTo(_users);

        var outcome = await useCase.UploadAsync(patient.Id, ProfilePhotoOwner.Patient, CreateImage(), TestContext.Current.CancellationToken);

        Assert.Equal(ProfilePhotoResult.ImageTooLarge, outcome.Result);
        Assert.Null(patient.PatientProfile!.PhotoPath);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Patient_cannot_upload_as_nutritionist()
    {
        var patient = TestUsers.Patient().AddTo(_users);

        var outcome = await _useCase.UploadAsync(patient.Id, ProfilePhotoOwner.Nutritionist, CreateImage(), TestContext.Current.CancellationToken);

        Assert.Equal(ProfilePhotoResult.ProfileNotFound, outcome.Result);
    }

    [Fact]
    public async Task Remove_clears_path_and_deletes_file()
    {
        var patient = TestUsers.Patient().AddTo(_users);
        await _useCase.UploadAsync(patient.Id, ProfilePhotoOwner.Patient, CreateImage(), TestContext.Current.CancellationToken);
        var path = patient.PatientProfile!.PhotoPath!;

        var result = await _useCase.RemoveAsync(patient.Id, ProfilePhotoOwner.Patient, TestContext.Current.CancellationToken);

        Assert.Equal(ProfilePhotoResult.Done, result);
        Assert.Null(patient.PatientProfile.PhotoPath);
        Assert.False(_storage.Contains(StorageBucket.PatientPhotos, path));
    }

    private static MemoryStream CreateImage()
    {
        using var bitmap = new SKBitmap(1200, 900);
        bitmap.Erase(SKColors.Coral);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return new MemoryStream(data.ToArray());
    }
}
