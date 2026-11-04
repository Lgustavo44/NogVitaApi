using System.Text;
using NogVita.Application.Anamneses;
using NogVita.Application.Documents;
using NogVita.Domain.Anamneses;
using NogVita.Infrastructure.Pdf;

namespace NogVita.UnitTests.Infrastructure.Pdf;

[Trait(TestCategories.Key, TestCategories.Slow)] // o QuestPDF carrega as fontes e gera um PDF de verdade
public class QuestPdfAnamnesisRendererTests
{
    [Fact]
    public void Should_Generate_A_Pdf_With_Every_Question_Type()
    {
        var text = Question.Create("Qual é o seu objetivo?", QuestionType.Text, true, null);
        var yesNo = Question.Create("Tem alguma alergia?", QuestionType.YesNo, true, null);
        var single = Question.Create("Quantas refeições faz por dia?", QuestionType.SingleChoice, false, ["2", "3", "4 ou mais"]);
        var multiple = Question.Create("Quais você evita?", QuestionType.MultipleChoice, false, ["Lactose", "Glúten", "Carne"]);

        var anamnesis = new AnamnesisResponse(
            Guid.NewGuid(), "Anamnese inicial", AnamnesisStatus.Submitted, Guid.NewGuid(), "Paciente Teste", Guid.NewGuid(), "Nutri Teste",
            new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc),
            [text, yesNo, single, multiple],
            [new Answer(text.Id, Text: "Emagrecer"), new Answer(yesNo.Id, YesNo: false), new Answer(multiple.Id, Options: ["Lactose", "Glúten"])]);
        var stamp = new DocumentStamp("ABCD-EFGH-JKLM", new DateTime(2026, 11, 4, 9, 30, 0), 3, "12345", "https://nogvita.vercel.app/verificar-documento/ABCDEFGHJKLM");

        var pdf = new QuestPdfAnamnesisRenderer().Render(anamnesis, stamp);

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));

        File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "nogvita-anamnese-teste.pdf"), pdf);
    }
}
