using System.Text;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;
using NogVita.Infrastructure.Pdf;
using UglyToad.PdfPig;

namespace NogVita.UnitTests.Infrastructure.Pdf;

[Trait(TestCategories.Key, TestCategories.Slow)] // o QuestPDF carrega as fontes e gera um PDF de verdade
public class QuestPdfNutritionPlanRendererTests
{
    private static readonly PlanPdfStamp Stamp = new("ABCD-EFGH-JKLM", new DateTime(2026, 10, 17, 14, 5, 0), 3, "12345", "https://nogvita.vercel.app/verificar-documento/ABCDEFGHJKLM");

    private static NutritionPlanDetailResponse Detail()
    {
        var plan = new NutritionPlanResponse(
            Guid.NewGuid(), Guid.NewGuid(), "Paciente Teste", Guid.NewGuid(), "Nutri Teste", "Plano de Teste",
            null, new DateOnly(2026, 10, 1), null, NutritionPlanStatus.Active, DateTime.UtcNow,
            2000m, 150m, null, null);

        var rice = new MealFoodResponse(
            Guid.NewGuid(), Guid.NewGuid(), 150m, "Arroz, tipo 1, cozido", null, FoodSource.Taco, "3",
            DateTime.UtcNow, 128.26m, 2.52m, 28.06m, 0.23m, 1.56m, 1.22m);

        return new NutritionPlanDetailResponse(
            plan,
            [new MealResponse(Guid.NewGuid(), "Almoço", "Comer devagar.", 1, new TimeOnly(12, 0), [rice])]);
    }

    private static string Text(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return string.Join(" ", document.GetPages().Select(page => page.Text));
    }

    [Fact]
    public void Should_Generate_A_Pdf_File()
    {
        var pdf = new QuestPdfNutritionPlanRenderer().Render(Detail(), Stamp);

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Contains("Proteínas", Text(pdf));

        File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "nogvita-teste.pdf"), pdf);
    }

    [Fact]
    public void Patient_Pdf_Should_Not_Show_Macros_When_Not_Released()
    {
        var text = Text(new QuestPdfNutritionPlanRenderer().Render(Detail().ForPatient(), Stamp));

        Assert.DoesNotContain("Prot.", text);
        Assert.DoesNotContain("Proteínas", text);
        Assert.DoesNotContain("Carb.", text);
        Assert.DoesNotContain("Gorduras", text);
        Assert.Contains("kcal", text);
        Assert.Contains("Fibras", text);
    }
}
