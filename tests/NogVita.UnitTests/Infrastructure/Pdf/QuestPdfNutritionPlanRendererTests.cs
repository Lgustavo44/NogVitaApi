using System.Text;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;
using NogVita.Infrastructure.Pdf;

namespace NogVita.UnitTests.Infrastructure.Pdf;

[Trait(TestCategories.Key, TestCategories.Slow)] // o QuestPDF carrega as fontes e gera um PDF de verdade
public class QuestPdfNutritionPlanRendererTests
{
    [Fact]
    public void Should_Generate_A_Pdf_File()
    {
        var plan = new NutritionPlanResponse(
            Guid.NewGuid(), Guid.NewGuid(), "Paciente Teste", Guid.NewGuid(), "Nutri Teste", "Plano de Teste",
            null, new DateOnly(2026, 10, 1), null, NutritionPlanStatus.Active, DateTime.UtcNow,
            2000m, null, null, null);

        var rice = new MealFoodResponse(
            Guid.NewGuid(), Guid.NewGuid(), 150m, "Arroz, tipo 1, cozido", null, FoodSource.Taco, "3",
            DateTime.UtcNow, 128.26m, 2.52m, 28.06m, 0.23m, 1.56m, 1.22m);

        var detail = new NutritionPlanDetailResponse(
            plan,
            [new MealResponse(Guid.NewGuid(), "Almoço", "Comer devagar.", 1, new TimeOnly(12, 0), [rice])]);

        var pdf = new QuestPdfNutritionPlanRenderer().Render(detail, new DateOnly(2026, 10, 17));

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));

        File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "nogvita-teste.pdf"), pdf);
    }
}