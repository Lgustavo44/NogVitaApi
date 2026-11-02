using NogVita.Application.Abstractions;
using SkiaSharp;

namespace NogVita.Infrastructure.Images;

internal sealed class SkiaImageProcessor(int maxPixels = SkiaImageProcessor.DefaultMaxPixels) : IImageProcessor
{
    // D-V2-07: 40 megapixels cobre qualquer câmera de celular, e barra a "bomba de descompressão".
    public const int DefaultMaxPixels = 40_000_000;
    private const int WebpQuality = 80;

    public ImageProcessingResult Process(Stream input, int maxSide)
    {
        using var codec = SKCodec.Create(input);

        // O codec só existe se os bytes forem mesmo uma imagem. Um .exe renomeado para .jpg cai aqui.
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
        {
            return new ImageProcessingResult(ImageProcessingStatus.InvalidImage);
        }

        // O tamanho vem do cabeçalho, ANTES de decodificar: um PNG de 50 KB pode declarar 30.000 x 30.000 pixels.
        if ((long)codec.Info.Width * codec.Info.Height > maxPixels)
        {
            return new ImageProcessingResult(ImageProcessingStatus.TooLarge);
        }

        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null)
        {
            return new ImageProcessingResult(ImageProcessingStatus.InvalidImage);
        }

        using var oriented = ApplyOrigin(decoded, codec.EncodedOrigin);
        using var resized = ResizeToFit(oriented, maxSide);
        using var image = SKImage.FromBitmap(resized);

        // O encoder do Skia não escreve EXIF: a localização GPS e o modelo do celular ficam para trás.
        using var data = image.Encode(SKEncodedImageFormat.Webp, WebpQuality);

        return new ImageProcessingResult(
            ImageProcessingStatus.Processed,
            new ProcessedImage(data.ToArray(), resized.Width, resized.Height));
    }

    // O celular grava a foto "deitada" e anota no EXIF como girar. Como o EXIF vai embora na recodificação,
    // a rotação precisa ser aplicada nos pixels; senão a foto aparece de lado.
    internal static SKBitmap ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return source.Copy();
        }

        var swapsSides = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var result = swapsSides
            ? new SKBitmap(source.Height, source.Width, source.ColorType, source.AlphaType)
            : new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);

        using var canvas = new SKCanvas(result);
        canvas.SetMatrix(OriginMatrix(origin, source.Width, source.Height));
        canvas.DrawBitmap(source, 0, 0);

        return result;
    }

    // Cada matriz leva o ponto (x, y) da imagem gravada para a posição dele na imagem em pé.
    // A ordem dos argumentos do SKMatrix: scaleX, skewX, transX, skewY, scaleY, transY, persp0, persp1, persp2.
    private static SKMatrix OriginMatrix(SKEncodedOrigin origin, float width, float height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),         // espelhada
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1), // 180°
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),       // espelhada na vertical
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),                // transposta
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),         // 90° horário
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1), // transposta inversa
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),        // 90° anti-horário
        SKEncodedOrigin.TopLeft => SKMatrix.Identity
    };

    private static SKBitmap ResizeToFit(SKBitmap source, int maxSide)
    {
        var largestSide = Math.Max(source.Width, source.Height);
        if (largestSide <= maxSide)
        {
            return source.Copy();
        }

        var scale = (double)maxSide / largestSide;
        var info = new SKImageInfo(
            (int)Math.Round(source.Width * scale),
            (int)Math.Round(source.Height * scale));

        return source.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
    }
}
