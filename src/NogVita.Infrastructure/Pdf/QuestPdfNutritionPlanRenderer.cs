using System.Globalization;
using System.Reflection;
using NogVita.Application.Foods;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.NutritionPlans;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NogVita.Infrastructure.Pdf;

// Layout no estilo dos e-mails (EmailLayout): faixa laranja no topo, a mesma logo, blocos de destaque cor de polpa.
// O fundo da página fica branco, e não cinza como no e-mail, para não gastar tinta na impressão.
internal sealed class QuestPdfNutritionPlanRenderer : INutritionPlanPdfRenderer
{
    private const string Orange = "#F28C28";
    private const string Pulp = "#FFF4E3";
    private const string DarkGreen = "#1D2A24";
    private const string Muted = "#5B6B63";
    private const string Light = "#F4F6F5";
    private const string Line = "#E3E8E5";

    private const string LogoResource = "NogVita.Email.logo.png";

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Lazy<byte[]> Logo = new(LoadLogo);

    public byte[] Render(NutritionPlanDetailResponse detail, DateOnly generatedOn) =>
        Create(detail, generatedOn).GeneratePdf();

    internal static IDocument Create(NutritionPlanDetailResponse detail, DateOnly generatedOn)
    {
        PdfFonts.Register();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(36);
                page.MarginTop(30);
                page.MarginBottom(24);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(text => text.FontFamily(PdfFonts.Family).FontSize(9).FontColor(DarkGreen));

                // A faixa laranja ocupa a largura toda da página, como no topo dos e-mails.
                page.Background().AlignTop().Height(8).Background(Orange);

                page.Header().Element(header => ComposeHeader(header, detail));
                page.Content().Element(content => ComposeContent(content, detail));
                page.Footer().Element(footer => ComposeFooter(footer, detail, generatedOn));
            });
        });
    }

    private static void ComposeHeader(IContainer container, NutritionPlanDetailResponse detail)
    {
        container.PaddingBottom(14).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(120).AlignMiddle().Image(Logo.Value).FitWidth();

                row.RelativeItem().AlignRight().AlignMiddle().Column(title =>
                {
                    title.Item().AlignRight().Text("PLANO ALIMENTAR").FontSize(8).Bold().FontColor(Muted).LetterSpacing(0.08f);
                    title.Item().AlignRight().Text(detail.Plan.Name).FontSize(14).Bold();
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Line);
        });
    }

    private static void ComposeContent(IContainer container, NutritionPlanDetailResponse detail)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            column.Item().Element(item => ComposePlanInfo(item, detail.Plan));

            if (detail.Plan.Description is { } description)
            {
                column.Item().Text(description).FontColor(Muted);
            }

            if (detail.Meals.Count == 0)
            {
                column.Item().Element(item => Notice(item, "Este plano ainda não tem refeições."));
            }

            foreach (var meal in detail.Meals)
            {
                column.Item().Element(item => ComposeMeal(item, meal));
            }

            column.Item().Element(item => ComposeDailyTotals(item, detail));

            if (detail.Target is { } target)
            {
                column.Item().Element(item => ComposeTarget(item, target));
            }
        });
    }

    private static void ComposePlanInfo(IContainer container, NutritionPlanResponse plan)
    {
        var period = plan.EndDate is { } end
            ? $"{plan.StartDate.ToString("dd/MM/yyyy", PtBr)} a {end.ToString("dd/MM/yyyy", PtBr)}"
            : $"A partir de {plan.StartDate.ToString("dd/MM/yyyy", PtBr)}";

        container.Background(Light).Padding(10).Row(row =>
        {
            InfoItem(row.RelativeItem(3), "Paciente", plan.PatientName);
            InfoItem(row.RelativeItem(3), "Nutricionista", plan.NutritionistName);
            InfoItem(row.RelativeItem(3), "Período", period);
            InfoItem(row.RelativeItem(2), "Situação", StatusLabel(plan.Status));
        });
    }

    private static void InfoItem(IContainer container, string label, string value)
    {
        container.PaddingRight(8).Column(column =>
        {
            column.Item().Text(label.ToUpperInvariant()).FontSize(7).Bold().FontColor(Muted).LetterSpacing(0.06f);
            column.Item().PaddingTop(2).Text(value).Bold();
        });
    }

    private static void ComposeMeal(IContainer container, MealResponse meal)
    {
        // Garante que o título da refeição não fique sozinho no pé da página.
        container.EnsureSpace(90).Column(column =>
        {
            column.Item().BorderLeft(4).BorderColor(Orange).PaddingLeft(8).Row(row =>
            {
                row.RelativeItem().AlignMiddle().Text(meal.Name).FontSize(12).Bold();

                if (meal.SuggestedTime is { } time)
                {
                    row.AutoItem().AlignMiddle().Background(Pulp).PaddingVertical(2).PaddingHorizontal(8)
                        .Text(time.ToString("HH:mm", PtBr)).Bold().FontColor(Orange);
                }
            });

            if (meal.Notes is { } notes)
            {
                column.Item().PaddingTop(4).PaddingLeft(12).Text(notes).Italic().FontColor(Muted);
            }

            if (meal.Foods.Count == 0)
            {
                column.Item().PaddingTop(6).PaddingLeft(12).Text("Nenhum alimento nesta refeição.").FontColor(Muted);
                return;
            }

            column.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Alimento", alignRight: false);
                    foreach (var label in new[] { "Quantidade", "kcal", "Prot. (g)", "Carb. (g)", "Gord. (g)", "Fibra (g)" })
                    {
                        HeaderCell(header.Cell(), label, alignRight: true);
                    }
                });

                foreach (var food in meal.Foods)
                {
                    var name = food.Brand is null ? food.Name : $"{food.Name} ({food.Brand})";

                    BodyCell(table.Cell()).Text(name);
                    BodyCell(table.Cell()).AlignRight().Text($"{food.QuantityInGrams.ToString("0.#", PtBr)} g");
                    BodyCell(table.Cell()).AlignRight().Text(Format(food.Portion.EnergyKcal));
                    BodyCell(table.Cell()).AlignRight().Text(Format(food.Portion.Protein));
                    BodyCell(table.Cell()).AlignRight().Text(Format(food.Portion.Carbohydrate));
                    BodyCell(table.Cell()).AlignRight().Text(Format(food.Portion.Fat));
                    BodyCell(table.Cell()).AlignRight().Text(Format(food.Portion.Fiber));
                }

                // Total da refeição: o rótulo ocupa as duas primeiras colunas (alimento e quantidade).
                TotalCell(table.Cell().ColumnSpan(2)).Text("Total da refeição").Bold();
                TotalCell(table.Cell()).AlignRight().Text(Format(meal.Totals.EnergyKcal)).Bold();
                TotalCell(table.Cell()).AlignRight().Text(Format(meal.Totals.Protein)).Bold();
                TotalCell(table.Cell()).AlignRight().Text(Format(meal.Totals.Carbohydrate)).Bold();
                TotalCell(table.Cell()).AlignRight().Text(Format(meal.Totals.Fat)).Bold();
                TotalCell(table.Cell()).AlignRight().Text(Format(meal.Totals.Fiber)).Bold();
            });
        });
    }

    private static void HeaderCell(IContainer cell, string label, bool alignRight)
    {
        var container = cell.Background(Pulp).PaddingVertical(4).PaddingHorizontal(6);
        (alignRight ? container.AlignRight() : container).Text(label).FontSize(8).Bold();
    }

    private static IContainer BodyCell(IContainer cell) =>
        cell.BorderBottom(1).BorderColor(Line).PaddingVertical(4).PaddingHorizontal(6);

    private static IContainer TotalCell(IContainer cell) =>
        cell.Background(Light).PaddingVertical(4).PaddingHorizontal(6);

    private static void ComposeDailyTotals(IContainer container, NutritionPlanDetailResponse detail)
    {
        var totals = detail.DailyTotals;
        var hasIncomplete = new[] { totals.EnergyKcal, totals.Protein, totals.Carbohydrate, totals.Fat, totals.Fiber, totals.SodiumMg }
            .Any(t => !t.IsComplete);

        // O mesmo bloco de destaque dos e-mails: fundo polpa e borda laranja à esquerda.
        container.EnsureSpace(80).Background(Pulp).BorderLeft(4).BorderColor(Orange).Padding(12).Column(column =>
        {
            column.Item().Text("Total do dia").FontSize(12).Bold();

            column.Item().PaddingTop(8).Row(row =>
            {
                Stat(row.RelativeItem(), Format(totals.EnergyKcal), "kcal", "Energia");
                Stat(row.RelativeItem(), Format(totals.Protein), "g", "Proteínas");
                Stat(row.RelativeItem(), Format(totals.Carbohydrate), "g", "Carboidratos");
                Stat(row.RelativeItem(), Format(totals.Fat), "g", "Gorduras");
                Stat(row.RelativeItem(), Format(totals.Fiber), "g", "Fibras");
                Stat(row.RelativeItem(), Format(totals.SodiumMg), "mg", "Sódio");
            });

            if (hasIncomplete)
            {
                column.Item().PaddingTop(8)
                    .Text("Valores com ≥ estão incompletos: algum alimento não informa esse nutriente, então o total real pode ser maior.")
                    .FontSize(7).FontColor(Muted);
            }
        });
    }

    private static void Stat(IContainer container, string value, string unit, string label)
    {
        container.Column(column =>
        {
            column.Item().Text(text =>
            {
                text.Span(value).FontSize(13).Bold();
                text.Span($" {unit}").FontSize(8).FontColor(Muted);
            });
            column.Item().Text(label).FontSize(8).FontColor(Muted);
        });
    }

    private static void ComposeTarget(IContainer container, TargetComparisonResponse target)
    {
        container.EnsureSpace(90).Column(column =>
        {
            column.Item().BorderLeft(4).BorderColor(Orange).PaddingLeft(8).Text("Meta diária").FontSize(12).Bold();

            column.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Nutriente", alignRight: false);
                    HeaderCell(header.Cell(), "Meta", alignRight: true);
                    HeaderCell(header.Cell(), "Planejado", alignRight: true);
                    HeaderCell(header.Cell(), "Diferença", alignRight: true);
                });

                TargetRow(table, "Energia", "kcal", target.EnergyKcal);
                TargetRow(table, "Proteínas", "g", target.Protein);
                TargetRow(table, "Carboidratos", "g", target.Carbohydrate);
                TargetRow(table, "Gorduras", "g", target.Fat);
            });
        });
    }

    private static void TargetRow(TableDescriptor table, string label, string unit, NutrientComparisonResponse? comparison)
    {
        // Sem meta para esse nutriente: a linha não aparece.
        if (comparison is null)
        {
            return;
        }

        var planned = (comparison.IsComplete ? string.Empty : "≥ ") + comparison.Planned.ToString("#,##0.0", PtBr);

        BodyCell(table.Cell()).Text(label);
        BodyCell(table.Cell()).AlignRight().Text($"{comparison.Target.ToString("#,##0.0", PtBr)} {unit}");
        BodyCell(table.Cell()).AlignRight().Text($"{planned} {unit}");
        BodyCell(table.Cell()).AlignRight().Text($"{FormatDifference(comparison.Difference)} {unit}").Bold();
    }

    private static void ComposeFooter(IContainer container, NutritionPlanDetailResponse detail, DateOnly generatedOn)
    {
        var sources = detail.Meals
            .SelectMany(m => m.Foods)
            .Select(f => f.Source)
            .Distinct()
            .Select(FoodSourceInfo.Attribution)
            .ToList();

        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Line);

            if (sources.Count > 0)
            {
                column.Item().PaddingTop(5).Text($"Fontes dos dados nutricionais: {string.Join("; ", sources)}")
                    .FontSize(7).FontColor(Muted);
            }

            column.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text($"NogVita · Acompanhamento nutricional · Gerado em {generatedOn.ToString("dd/MM/yyyy", PtBr)}")
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
        });
    }

    private static void Notice(IContainer container, string text) =>
        container.Background(Pulp).BorderLeft(4).BorderColor(Orange).Padding(10).Text(text);

    private static string StatusLabel(NutritionPlanStatus status) => status switch
    {
        NutritionPlanStatus.Draft => "Rascunho",
        NutritionPlanStatus.Active => "Ativo",
        NutritionPlanStatus.Finished => "Finalizado",
        NutritionPlanStatus.Cancelled => "Cancelado",
    };

    private static string Format(decimal? value) =>
        value?.ToString("#,##0.0", PtBr) ?? "—";

    private static string Format(NutrientTotalResponse total) =>
        (total.IsComplete ? string.Empty : "≥ ") + total.Value.ToString("#,##0.0", PtBr);

    private static string FormatDifference(decimal difference) =>
        difference > 0 ? $"+{difference.ToString("#,##0.0", PtBr)}" : difference.ToString("#,##0.0", PtBr);

    private static byte[] LoadLogo()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResource)
            ?? throw new InvalidOperationException($"A logo embutida '{LogoResource}' não foi encontrada.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
