using NogVita.Application.Anamneses;
using NogVita.Application.Common;
using NogVita.Application.Documents;
using NogVita.Domain.Anamneses;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static NogVita.Infrastructure.Pdf.PdfBrand;

namespace NogVita.Infrastructure.Pdf;

// As perguntas na ordem do questionário, cada uma com a resposta. A pergunta sem resposta (opcional) aparece assim mesmo.
internal sealed class QuestPdfAnamnesisRenderer : IAnamnesisPdfRenderer
{
    public byte[] Render(AnamnesisResponse anamnesis, DocumentStamp stamp) =>
        Create(anamnesis, stamp).GeneratePdf();

    internal static IDocument Create(AnamnesisResponse anamnesis, DocumentStamp stamp)
    {
        PdfFonts.Register();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                SetupPage(page);

                page.Header().Element(header => Header(header, "Anamnese", anamnesis.Title));
                page.Content().Element(content => ComposeContent(content, anamnesis, stamp));
                page.Footer().Column(column =>
                {
                    column.Item().LineHorizontal(1).LineColor(Line);
                    StampLine(column, stamp);
                });
            });
        });
    }

    private static void ComposeContent(IContainer container, AnamnesisResponse anamnesis, DocumentStamp stamp)
    {
        var answers = (anamnesis.Answers ?? []).ToDictionary(a => a.QuestionId);

        container.Column(column =>
        {
            column.Spacing(12);

            column.Item().Background(Light).Padding(10).Row(row =>
            {
                InfoItem(row.RelativeItem(3), "Paciente", anamnesis.PatientName);
                InfoItem(row.RelativeItem(3), "Nutricionista", Nutritionist(anamnesis.NutritionistName, stamp));
                InfoItem(row.RelativeItem(2), "Enviada em", Date(anamnesis.SentAt));
                InfoItem(row.RelativeItem(2), "Respondida em", anamnesis.SubmittedAt is { } submitted ? Date(submitted) : "—");
            });

            for (var index = 0; index < anamnesis.Questions.Count; index++)
            {
                var question = anamnesis.Questions[index];
                var answer = answers.GetValueOrDefault(question.Id);
                var number = index + 1;

                column.Item().EnsureSpace(50).BorderLeft(4).BorderColor(Orange).PaddingLeft(8).Column(item =>
                {
                    item.Item().Text($"{number}. {question.Text}").Bold();

                    var text = AnswerText(question, answer);
                    var line = item.Item().PaddingTop(3).Text(text ?? "Sem resposta");
                    if (text is null)
                    {
                        line.Italic().FontColor(Muted);
                    }
                });
            }
        });
    }

    private static string? AnswerText(Question question, Answer? answer) => answer is null
        ? null
        : question.Type switch
        {
            QuestionType.Text => answer.Text,
            QuestionType.YesNo => answer.YesNo is { } yes ? (yes ? "Sim" : "Não") : null,
            _ => answer.Options is { Count: > 0 } options ? string.Join("; ", options) : null,
        };

    private static string Date(DateTime utc) => BrazilianCalendar.ToBrasilia(utc).ToString("dd/MM/yyyy", PtBr);
}
