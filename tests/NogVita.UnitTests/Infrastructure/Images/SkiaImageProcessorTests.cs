using NogVita.Application.Abstractions;
using NogVita.Infrastructure.Images;
using SkiaSharp;

namespace NogVita.UnitTests.Infrastructure.Images;

public class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _processor = new();

    [Fact]
    public void Large_photo_is_reduced_to_max_side_and_encoded_as_webp()
    {
        using var input = CreateImage(4000, 3000, SKEncodedImageFormat.Jpeg);

        var result = _processor.Process(input, maxSide: 1600);

        Assert.Equal(ImageProcessingStatus.Processed, result.Status);
        Assert.Equal(1600, result.Image!.Width);
        Assert.Equal(1200, result.Image.Height);

        using var codec = SKCodec.Create(new MemoryStream(result.Image.Content));
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
    }

    [Fact]
    public void Small_photo_keeps_its_size()
    {
        using var input = CreateImage(800, 600, SKEncodedImageFormat.Png);

        var result = _processor.Process(input, maxSide: 1600);

        Assert.Equal((800, 600), (result.Image!.Width, result.Image.Height));
    }

    [Fact]
    public void Bytes_that_are_not_an_image_are_rejected()
    {
        using var input = new MemoryStream("MZ isto não é uma imagem"u8.ToArray());

        var result = _processor.Process(input, maxSide: 1600);

        Assert.Equal(ImageProcessingStatus.InvalidImage, result.Status);
        Assert.Null(result.Image);
    }

    [Fact]
    public void Image_above_pixel_limit_is_rejected_before_decoding()
    {
        var processor = new SkiaImageProcessor(maxPixels: 100 * 100);
        using var input = CreateImage(101, 100, SKEncodedImageFormat.Png);

        var result = processor.Process(input, maxSide: 1600);

        Assert.Equal(ImageProcessingStatus.TooLarge, result.Status);
    }

    [Fact]
    public void RightTop_origin_rotates_clockwise()
    {
        // Uma imagem 2x1: o pixel da esquerda vermelho, o da direita azul.
        using var source = new SKBitmap(2, 1);
        source.SetPixel(0, 0, SKColors.Red);
        source.SetPixel(1, 0, SKColors.Blue);

        using var rotated = SkiaImageProcessor.ApplyOrigin(source, SKEncodedOrigin.RightTop);

        // Girada 90° no sentido horário, ela fica em pé (1x2): o vermelho em cima, o azul embaixo.
        Assert.Equal((1, 2), (rotated.Width, rotated.Height));
        Assert.Equal(SKColors.Red, rotated.GetPixel(0, 0));
        Assert.Equal(SKColors.Blue, rotated.GetPixel(0, 1));
    }

    private static MemoryStream CreateImage(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Teal);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);

        return new MemoryStream(data.ToArray());
    }
}
