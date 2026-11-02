namespace NogVita.Application.Abstractions;

public enum ImageProcessingStatus
{
    Processed,
    InvalidImage,
    TooLarge
}

public sealed record ProcessedImage(byte[] Content, int Width, int Height)
{
    public const string ContentType = "image/webp";
}

public sealed record ImageProcessingResult(ImageProcessingStatus Status, ProcessedImage? Image = null);

public interface IImageProcessor
{
    ImageProcessingResult Process(Stream input, int maxSide);
}
