using System.Reflection;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace NogVita.Infrastructure.Pdf;

internal static class PdfFonts
{
    public const string Family = "Roboto";

    private static readonly string[] Resources =
    [
        "NogVita.Infrastructure.Fonts.Roboto-Regular.ttf",
        "NogVita.Infrastructure.Fonts.Roboto-Bold.ttf"
    ];

    private static int _registered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;
        }

        QuestPDF.Settings.License = LicenseType.Community;

        foreach (var resource in Resources)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"A fonte embutida '{resource}' não foi encontrada.");

            FontManager.RegisterFontFromStream(stream);
        }
    }
}
