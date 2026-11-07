using NogVita.Domain.Common;
using NogVita.Domain.FoodDiary;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Domain.FoodDiary;

public class FoodDiaryDomainTests
{
    private static readonly DateOnly Today = new(2026, 11, 7); // um sábado

    private static Meal Lunch()
    {
        var plan = new NutritionPlan(Guid.NewGuid(), Guid.NewGuid(), "Plano", null, new DateOnly(2026, 11, 1), null);
        return plan.AddMeal("Almoço", null);
    }

    private static readonly NutrientAmounts Planned = new(500m, 30m, 60m, 15m, 8m, 400m);

    // D-V2-37: de hoje até 7 dias atrás.
    [Theory]
    [InlineData(1)]
    [InlineData(-8)]
    public void Day_Should_Be_Within_The_Window(int days)
    {
        Assert.Throws<DomainException>(() => new DiaryDay(Guid.NewGuid(), Today.AddDays(days), Today));
    }

    [Fact]
    public void Checking_In_Again_Should_Replace_The_Previous_Answer()
    {
        var day = new DiaryDay(Guid.NewGuid(), Today, Today);
        var lunch = Lunch();

        day.CheckIn(lunch, Planned, MealAdherence.Partial, "  Comi metade  ", Today);
        day.CheckIn(lunch, Planned, MealAdherence.Followed, null, Today);

        var checkIn = Assert.Single(day.MealCheckIns);
        Assert.Equal(MealAdherence.Followed, checkIn.Adherence);
        Assert.Null(checkIn.Notes);
    }

    [Fact]
    public void Extras_Should_Keep_A_Snapshot_And_Respect_The_Limit()
    {
        var day = new DiaryDay(Guid.NewGuid(), Today, Today);
        var cake = Food.FromTaco(500, "Bolo, pronto, chocolate", null, new NutrientsPer100g(410m, 6.2m, 54.7m, 18.5m, 1.4m, 283m));

        var extra = day.AddExtra(cake, FoodQuantity.InGrams(80m), mealId: null, Today);

        Assert.Equal("Bolo, pronto, chocolate", extra.Name);
        Assert.Equal(328m, extra.ComputeAmounts().EnergyKcal);

        for (var i = 1; i < DiaryDay.MaxExtraFoods; i++)
        {
            day.AddExtra(cake, FoodQuantity.InGrams(10m), null, Today);
        }

        Assert.Throws<DomainException>(() => day.AddExtra(cake, FoodQuantity.InGrams(10m), null, Today));
    }

    // D-V2-39
    [Fact]
    public void Week_Should_Start_On_Monday_And_Lock_After_Feedback()
    {
        Assert.Throws<DomainException>(() => new WeeklyCheckIn(Guid.NewGuid(), null, Today, Today));

        var monday = WeeklyCheckIn.WeekOf(Today);
        Assert.Equal(new DateOnly(2026, 11, 2), monday);

        var week = new WeeklyCheckIn(Guid.NewGuid(), Guid.NewGuid(), monday, Today);
        week.Answer(3, 4, 2, "Fome à noite", null, null);
        week.GiveFeedback(Guid.NewGuid(), "Vamos ajustar o lanche da tarde.", DateTime.UtcNow);

        Assert.Throws<DomainException>(() => week.Answer(4, 4, 4, null, null, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Scores_Should_Go_From_One_To_Five(int score)
    {
        var week = new WeeklyCheckIn(Guid.NewGuid(), null, WeeklyCheckIn.WeekOf(Today), Today);

        Assert.Throws<DomainException>(() => week.Answer(score, 3, 3, null, null, null));
    }
}
