using NogVita.Domain.Users;

namespace NogVita.UnitTests.Domain.Users;

public class AwakeWindowTests
{
    private static TimeOnly T(string time) => TimeOnly.Parse(time);

    [Theory]
    // Rotina comum: acorda 06:00, dorme 22:30
    [InlineData("12:00", "06:00", "22:30", false)]
    [InlineData("06:00", "06:00", "22:30", false)] // na hora de acordar: dentro
    [InlineData("22:30", "06:00", "22:30", false)] // na hora de dormir: dentro
    [InlineData("05:30", "06:00", "22:30", true)]  // antes de acordar
    [InlineData("23:30", "06:00", "22:30", true)]  // ceia depois de dormir
    [InlineData("03:00", "06:00", "22:30", true)]  // madrugada
    // Turno da noite: acorda 14:00, dorme 06:00 (a janela atravessa a meia-noite)
    [InlineData("03:00", "14:00", "06:00", false)] // jantar de madrugada: dentro
    [InlineData("20:00", "14:00", "06:00", false)]
    [InlineData("00:00", "14:00", "06:00", false)]
    [InlineData("14:00", "14:00", "06:00", false)] // na hora de acordar: dentro
    [InlineData("06:00", "14:00", "06:00", false)] // na hora de dormir: dentro
    [InlineData("10:00", "14:00", "06:00", true)]  // dormindo
    [InlineData("06:30", "14:00", "06:00", true)]
    public void Should_Tell_If_Time_Is_Outside_Awake_Window(string time, string wakeUp, string bed, bool expected)
    {
        Assert.Equal(expected, AwakeWindow.IsOutside(T(time), T(wakeUp), T(bed)));
    }

    [Fact]
    public void Should_Be_Unknown_Without_Meal_Time()
    {
        Assert.Null(AwakeWindow.IsOutside(null, T("06:00"), T("22:30")));
    }

    [Fact]
    public void Should_Be_Unknown_Without_Full_Routine()
    {
        Assert.Null(AwakeWindow.IsOutside(T("12:00"), null, T("22:30")));
        Assert.Null(AwakeWindow.IsOutside(T("12:00"), T("06:00"), null));
    }
}
