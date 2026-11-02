using NogVita.Domain.Common;
using NogVita.Domain.ProgressPhotos;

namespace NogVita.UnitTests.Domain.ProgressPhotos;

public class ProgressPhotoTests
{
    private static readonly DateOnly Today = new(2026, 11, 2);
    private static readonly Guid PatientId = Guid.NewGuid();

    [Fact]
    public void Future_date_is_rejected() =>
        Assert.Throws<DomainException>(() => new ProgressPhoto(PatientId, Today.AddDays(1), ProgressPhotoAngle.Front, "p/1.webp", Today));

    [Fact]
    public void Date_older_than_limit_is_rejected() =>
        Assert.Throws<DomainException>(() =>
            new ProgressPhoto(PatientId, Today.AddDays(-ProgressPhoto.MaxDaysInPast - 1), ProgressPhotoAngle.Front, "p/1.webp", Today));

    [Fact]
    public void Date_at_limit_is_accepted()
    {
        var photo = new ProgressPhoto(PatientId, Today.AddDays(-ProgressPhoto.MaxDaysInPast), ProgressPhotoAngle.Back, "p/1.webp", Today);

        Assert.Equal(ProgressPhotoAngle.Back, photo.Angle);
    }

    [Fact]
    public void Undefined_angle_is_rejected() =>
        Assert.Throws<DomainException>(() => new ProgressPhoto(PatientId, Today, (ProgressPhotoAngle)7, "p/1.webp", Today));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Empty_path_is_a_bug(string path) =>
        Assert.Throws<ArgumentException>(() => new ProgressPhoto(PatientId, Today, ProgressPhotoAngle.Front, path, Today));

    [Fact]
    public void Path_longer_than_limit_is_a_bug() =>
        Assert.Throws<ArgumentException>(() =>
            new ProgressPhoto(PatientId, Today, ProgressPhotoAngle.Front, new string('a', ProgressPhoto.StoragePathMaxLength + 1), Today));

    [Fact]
    public void ReplaceFile_returns_previous_path()
    {
        var photo = new ProgressPhoto(PatientId, Today, ProgressPhotoAngle.Side, "p/antiga.webp", Today);

        var previous = photo.ReplaceFile("p/nova.webp");

        Assert.Equal("p/antiga.webp", previous);
        Assert.Equal("p/nova.webp", photo.StoragePath);
    }
}
