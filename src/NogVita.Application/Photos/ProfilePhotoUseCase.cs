using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Domain.Users;

namespace NogVita.Application.Photos;

public enum ProfilePhotoOwner
{
    Nutritionist,
    Patient
}

public enum ProfilePhotoResult
{
    Done,
    ProfileNotFound,
    InvalidImage,
    ImageTooLarge
}

public sealed record ProfilePhotoOutcome(ProfilePhotoResult Result, string? PhotoUrl = null);

public sealed record ProfilePhotoResponse(string PhotoUrl);

public sealed class ProfilePhotoUseCase(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IImageProcessor imageProcessor,
    IFileStorage fileStorage,
    ILogger<ProfilePhotoUseCase> logger)
{
    // D-V2-07: a foto de perfil aparece pequena; 512 px cobre as telas de alta densidade.
    public const int MaxSide = 512;

    public async Task<ProfilePhotoOutcome> UploadAsync(Guid userId, ProfilePhotoOwner owner, Stream content, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (!HasProfile(user, owner))
        {
            return new ProfilePhotoOutcome(ProfilePhotoResult.ProfileNotFound);
        }

        var processed = imageProcessor.Process(content, MaxSide);
        if (processed.Status != ImageProcessingStatus.Processed)
        {
            return new ProfilePhotoOutcome(processed.Status == ImageProcessingStatus.TooLarge
                ? ProfilePhotoResult.ImageTooLarge
                : ProfilePhotoResult.InvalidImage);
        }

        var bucket = BucketOf(owner);
        var newPath = NewPath(user!.Id, owner);
        await fileStorage.UploadAsync(bucket, newPath, processed.Image!.Content, ProcessedImage.ContentType, cancellationToken);

        string? previousPath;
        try
        {
            previousPath = ChangePhoto(user, owner, newPath);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // O banco não gravou: sem isso, o arquivo novo ficaria órfão no bucket.
            await TryDeleteAsync(bucket, newPath);
            throw;
        }

        // Só depois de o banco confirmar. Apagar antes deixaria o perfil apontando para um arquivo que não existe.
        if (previousPath is not null)
        {
            await TryDeleteAsync(bucket, previousPath);
        }

        return new ProfilePhotoOutcome(ProfilePhotoResult.Done, await UrlOfAsync(bucket, newPath, cancellationToken));
    }

    public async Task<ProfilePhotoResult> RemoveAsync(Guid userId, ProfilePhotoOwner owner, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (!HasProfile(user, owner))
        {
            return ProfilePhotoResult.ProfileNotFound;
        }

        var previousPath = ChangePhoto(user!, owner, null);
        if (previousPath is null)
        {
            // Já estava sem foto: remover de novo não é erro.
            return ProfilePhotoResult.Done;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await TryDeleteAsync(BucketOf(owner), previousPath);
        return ProfilePhotoResult.Done;
    }

    private static bool HasProfile(User? user, ProfilePhotoOwner owner) => owner switch
    {
        ProfilePhotoOwner.Nutritionist => user is { IsActive: true, NutritionistProfile.IsActive: true },
        ProfilePhotoOwner.Patient => user is { IsActive: true, PatientProfile: not null }
    };

    private static string? ChangePhoto(User user, ProfilePhotoOwner owner, string? path) => owner switch
    {
        ProfilePhotoOwner.Nutritionist => user.ChangeNutritionistPhoto(path),
        ProfilePhotoOwner.Patient => user.ChangePatientPhoto(path)
    };

    private static StorageBucket BucketOf(ProfilePhotoOwner owner) => owner switch
    {
        ProfilePhotoOwner.Nutritionist => StorageBucket.NutritionistPhotos,
        ProfilePhotoOwner.Patient => StorageBucket.PatientPhotos
    };

    // D-V2-10 e D-V2-12: o caminho é gerado aqui, e um nome novo a cada envio fura o cache da foto antiga.
    private static string NewPath(Guid userId, ProfilePhotoOwner owner) => owner switch
    {
        ProfilePhotoOwner.Nutritionist => $"{userId}/{Guid.NewGuid()}.webp",
        ProfilePhotoOwner.Patient => $"{userId}/profile/{Guid.NewGuid()}.webp"
    };

    private async Task<string?> UrlOfAsync(StorageBucket bucket, string path, CancellationToken cancellationToken) => bucket switch
    {
        StorageBucket.NutritionistPhotos => fileStorage.GetPublicUrl(bucket, path),
        StorageBucket.PatientPhotos => (await fileStorage.CreateSignedUrlsAsync(bucket, [path], cancellationToken)).GetValueOrDefault(path)
    };

    private async Task TryDeleteAsync(StorageBucket bucket, string path)
    {
        try
        {
            // CancellationToken.None: a limpeza termina mesmo se o usuário fechar a página no meio.
            await fileStorage.DeleteAsync(bucket, [path], CancellationToken.None);
        }
        catch (Exception exception)
        {
            // D40: o log leva só o caminho, que tem apenas IDs.
            logger.LogWarning(exception, "Não foi possível apagar a foto {Path} do bucket {Bucket}.", path, bucket);
        }
    }
}
