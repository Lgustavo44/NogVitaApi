using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.FollowUps;
using NogVita.Application.Patients;
using NogVita.Application.Photos;
using NogVita.Domain.ProgressPhotos;
using NogVita.Domain.Users;

namespace NogVita.Application.ProgressPhotos;

public enum ProgressPhotoResult
{
    Done,
    ProfileNotFound,
    NotFound,
    ConsentRequired,
    OutdatedConsentVersion,
    LimitReached,
    InvalidImage,
    ImageTooLarge
}

public sealed record ProgressPhotoOutcome<T>(ProgressPhotoResult Result, T? Value = default);

public sealed record ProgressPhotoResponse(Guid Id, DateOnly TakenOn, ProgressPhotoAngle Angle, string? Url);

public sealed record ProgressPhotoDayResponse(DateOnly TakenOn, IReadOnlyList<ProgressPhotoResponse> Photos);

public sealed record ProgressPhotoConsentResponse(string Version, string Text, bool Accepted, DateTime? AcceptedAtUtc);

public sealed record AcceptProgressPhotoConsentRequest(string Version);

public sealed class ProgressPhotoUseCase(
    IUserRepository userRepository,
    IProgressPhotoRepository photoRepository,
    IUnitOfWork unitOfWork,
    IImageProcessor imageProcessor,
    IFileStorage fileStorage,
    CareAccessService careAccess,
    TimeProvider timeProvider,
    ILogger<ProgressPhotoUseCase> logger)
{
    // D-V2-07: grande o bastante para comparar detalhes entre duas datas.
    public const int MaxSide = 1600;

    public async Task<ProgressPhotoOutcome<ProgressPhotoConsentResponse>> GetConsentAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await ActivePatientAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return new(ProgressPhotoResult.ProfileNotFound);
        }

        var profile = patient.PatientProfile!;
        var accepted = profile.HasProgressPhotosConsent(ProgressPhotoConsent.CurrentVersion);

        return new(ProgressPhotoResult.Done, new ProgressPhotoConsentResponse(
            ProgressPhotoConsent.CurrentVersion,
            ProgressPhotoConsent.Text,
            accepted,
            accepted ? profile.ProgressPhotosConsentAtUtc : null));
    }

    public async Task<ProgressPhotoResult> AcceptConsentAsync(Guid patientId, string version, CancellationToken cancellationToken = default)
    {
        var patient = await ActivePatientAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return ProgressPhotoResult.ProfileNotFound;
        }

        // O paciente aceita o texto que leu. Se o termo mudou enquanto a tela estava aberta, ele precisa ler o novo.
        if (version != ProgressPhotoConsent.CurrentVersion)
        {
            return ProgressPhotoResult.OutdatedConsentVersion;
        }

        patient.GrantProgressPhotosConsent(version, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProgressPhotoResult.Done;
    }

    public async Task<ProgressPhotoResult> RevokeConsentAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await ActivePatientAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return ProgressPhotoResult.ProfileNotFound;
        }

        var photos = await photoRepository.ListForPatientAsync(patientId, cancellationToken);
        var paths = photos.Select(p => p.StoragePath).ToList();

        photoRepository.RemoveRange(photos);
        patient.RevokeProgressPhotosConsent();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // D-V2-17: os arquivos saem depois do banco. Se algum falhar, ele fica órfão, sem nenhum registro que leve até ele.
        await fileStorage.TryDeleteAsync(StorageBucket.PatientPhotos, paths, logger);
        return ProgressPhotoResult.Done;
    }

    public async Task<ProgressPhotoOutcome<ProgressPhotoResponse>> UploadAsync(
        Guid patientId, DateOnly takenOn, ProgressPhotoAngle angle, Stream content, CancellationToken cancellationToken = default)
    {
        var patient = await ActivePatientAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return new(ProgressPhotoResult.ProfileNotFound);
        }

        HealthDataConsentGuard.EnsureAccepted(patient);

        if (!patient.PatientProfile!.HasProgressPhotosConsent(ProgressPhotoConsent.CurrentVersion))
        {
            return new(ProgressPhotoResult.ConsentRequired);
        }

        var existing = await photoRepository.GetAsync(patientId, takenOn, angle, cancellationToken);
        if (existing is null && await photoRepository.CountForPatientAsync(patientId, cancellationToken) >= ProgressPhoto.MaxPerPatient)
        {
            return new(ProgressPhotoResult.LimitReached);
        }

        // D-V2-10 e D-V2-12: o caminho é gerado aqui, dentro da pasta do paciente.
        var newPath = $"{patientId}/progress/{Guid.NewGuid()}.webp";

        // A entidade nasce ANTES do envio: uma data ou um ângulo inválido para aqui (422), sem arquivo órfão no bucket.
        var photo = existing ?? new ProgressPhoto(patientId, takenOn, angle, newPath, BrazilianCalendar.Today(timeProvider));

        var processed = imageProcessor.Process(content, MaxSide);
        if (processed.Status != ImageProcessingStatus.Processed)
        {
            return new(processed.Status == ImageProcessingStatus.TooLarge
                ? ProgressPhotoResult.ImageTooLarge
                : ProgressPhotoResult.InvalidImage);
        }

        await fileStorage.UploadAsync(StorageBucket.PatientPhotos, newPath, processed.Image!.Content, ProcessedImage.ContentType, cancellationToken);

        string? previousPath = null;
        try
        {
            if (existing is null)
            {
                photoRepository.Add(photo);
            }
            else
            {
                previousPath = existing.ReplaceFile(newPath);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // O banco não gravou (por exemplo, o 409 de duas abas enviando o mesmo ângulo ao mesmo tempo).
            await fileStorage.TryDeleteAsync(StorageBucket.PatientPhotos, [newPath], logger);
            throw;
        }

        if (previousPath is not null)
        {
            await fileStorage.TryDeleteAsync(StorageBucket.PatientPhotos, [previousPath], logger);
        }

        var url = await fileStorage.PatientPhotoUrlAsync(newPath, cancellationToken);
        return new(ProgressPhotoResult.Done, new ProgressPhotoResponse(photo.Id, photo.TakenOn, photo.Angle, url));
    }

    public async Task<ProgressPhotoResult> DeleteAsync(Guid patientId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var photo = await photoRepository.GetByIdAsync(photoId, cancellationToken);

        // A foto de outra pessoa responde igual à que não existe: a API não confirma que o ID existe.
        if (photo is null || photo.PatientId != patientId)
        {
            return ProgressPhotoResult.NotFound;
        }

        photoRepository.Remove(photo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await fileStorage.TryDeleteAsync(StorageBucket.PatientPhotos, [photo.StoragePath], logger);
        return ProgressPhotoResult.Done;
    }

    public async Task<ProgressPhotoOutcome<IReadOnlyList<ProgressPhotoDayResponse>>> ListMineAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        if (await ActivePatientAsync(patientId, cancellationToken) is null)
        {
            return new(ProgressPhotoResult.ProfileNotFound);
        }

        return new(ProgressPhotoResult.Done, await ListAsync(patientId, cancellationToken));
    }

    public async Task<ProgressPhotoOutcome<IReadOnlyList<ProgressPhotoDayResponse>>> ListForNutritionistAsync(
        Guid nutritionistId, Guid patientId, CancellationToken cancellationToken = default)
    {
        // D-V2-06: sem vínculo ativo, para o nutricionista as fotos "não existem".
        if (!await careAccess.NutritionistFollowsPatientAsync(nutritionistId, patientId, cancellationToken))
        {
            return new(ProgressPhotoResult.NotFound);
        }

        return new(ProgressPhotoResult.Done, await ListAsync(patientId, cancellationToken));
    }

    private async Task<IReadOnlyList<ProgressPhotoDayResponse>> ListAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var photos = await photoRepository.ListForPatientAsync(patientId, cancellationToken);

        // Todas as URLs numa chamada só (no máximo 90 fotos, D-V2-14).
        var urls = await fileStorage.SignPatientPhotosAsync(photos.Select(p => p.StoragePath), cancellationToken);

        return photos
            .GroupBy(p => p.TakenOn)
            .OrderByDescending(day => day.Key)
            .Select(day => new ProgressPhotoDayResponse(
                day.Key,
                day.OrderBy(p => p.Angle)
                    .Select(p => new ProgressPhotoResponse(p.Id, p.TakenOn, p.Angle, urls.UrlFor(p.StoragePath)))
                    .ToList()))
            .ToList();
    }

    private async Task<User?> ActivePatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(patientId, cancellationToken);
        return user is { IsActive: true, PatientProfile: not null } ? user : null;
    }
}
