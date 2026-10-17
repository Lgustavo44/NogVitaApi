using NogVita.Infrastructure.Persistence.Queries;

namespace NogVita.UnitTests.Infrastructure.Foods;

public class FoodSearchWordsTests
{
    [Theory]
    [InlineData("coca zero", new[] { "coca", "zero" })]
    [InlineData("  coca   zero  ", new[] { "coca", "zero" })]
    [InlineData("coca-cola zero", new[] { "coca", "cola", "zero" })]
    [InlineData("arroz, tipo 1", new[] { "arroz", "tipo", "1" })]
    [InlineData("Coca coca COCA", new[] { "Coca" })]
    public void Should_Split_Search_Into_Words(string search, string[] expected)
    {
        Assert.Equal(expected, FoodQueries.SearchWords(search));
    }

    [Fact]
    public void Should_Limit_The_Number_Of_Words()
    {
        var words = FoodQueries.SearchWords("a b c d e f g h i j");

        Assert.Equal(8, words.Count);
    }

    [Fact]
    public void Should_Keep_Original_Text_When_It_Has_Only_Separators()
    {
        Assert.Equal(["--"], FoodQueries.SearchWords("--"));
    }
}
