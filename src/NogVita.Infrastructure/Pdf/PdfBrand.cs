using System.Globalization;
using System.Reflection;
using NogVita.Application.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NogVita.Infrastructure.Pdf;

// O visual comum dos PDFs (plano e anamnese), no estilo dos e-mails (EmailLayout): faixa laranja no topo, a mesma logo,
// blocos de destaque cor de polpa. O fundo fica branco, e não cinza como no e-mail, para não gastar tinta na impressão.
internal static class PdfBrand
{
    public const string Orange = "#F28C28";
    public const string Pulp = "#FFF4E3";
    public const string DarkGreen = "#1D2A24";
    public const string Muted = "#5B6B63";
    public const string Light = "#F4F6F5";
    public const string Line = "#E3E8E5";

    private const string LogoResource = "NogVita.Email.logo.png";

    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Lazy<byte[]> Logo = new(LoadLogo);

    public static void SetupPage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(36);
        page.MarginTop(30);
        page.MarginBottom(24);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(text => text.FontFamily(PdfFonts.Family).FontSize(9).FontColor(DarkGreen));

        // A faixa laranja ocupa a largura toda da página, como no topo dos e-mails.
        page.Background().AlignTop().Height(8).Background(Orange);
    }

    public static void Header(IContainer container, string kind, string title)
    {
        container.PaddingBottom(14).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(120).AlignMiddle().Image(Logo.Value).FitWidth();

                row.RelativeItem().AlignRight().AlignMiddle().Column(heading =>
                {
                    heading.Item().AlignRight().Text(kind.ToUpperInvariant()).FontSize(8).Bold().FontColor(Muted).LetterSpacing(0.08f);
                    heading.Item().AlignRight().Text(title).FontSize(14).Bold();
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Line);
        });
    }

    public static void InfoItem(IContainer container, string label, string value)
    {
        container.PaddingRight(8).Column(column =>
        {
            column.Item().Text(label.ToUpperInvariant()).FontSize(7).Bold().FontColor(Muted).LetterSpacing(0.06f);
            column.Item().PaddingTop(2).Text(value).Bold();
        });
    }

    public static string Nutritionist(string name, DocumentStamp stamp) => $"{name}\nCRN-{stamp.CrnRegion} {stamp.CrnNumber}";

    // A última linha do rodapé: a emissão, o código verificador e a página.
    public static void StampLine(ColumnDescriptor column, DocumentStamp stamp)
    {
        column.Item().PaddingTop(3).Row(row =>
        {
            row.RelativeItem().Text(
                    $"NogVita · Gerado em {stamp.IssuedAtBrasilia.ToString("dd/MM/yyyy 'às' HH:mm", PtBr)} (horário de Brasília) · " +
                    $"Código verificador: {stamp.VerificationCode} · Confira em {stamp.VerificationUrl}")
                .FontSize(7).FontColor(Muted);

            row.AutoItem().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(7).FontColor(Muted));
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        });
    }

    public static void Notice(IContainer container, string text) =>
        container.Background(Pulp).BorderLeft(4).BorderColor(Orange).Padding(10).Text(text);

    private static byte[] LoadLogo()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResource)
            ?? throw new InvalidOperationException($"A logo embutida '{LogoResource}' não foi encontrada.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
