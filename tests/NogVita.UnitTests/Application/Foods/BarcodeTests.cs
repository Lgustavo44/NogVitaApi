using NogVita.Domain.Foods;

namespace NogVita.UnitTests.Application.Foods;

public class BarcodeTests
{
    [Theory]
    [InlineData("78912345")]
    [InlineData("012345678905")]
    [InlineData("7891000100103")]
    [InlineData("17891000100100")]
    public void Should_Accept_Valid_Lengths(string value)
    {
        Assert.True(Barcode.IsValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("78910001001")]
    [InlineData("78910001001A3")]
    [InlineData("7891000 00103")]
    [InlineData("789100010010312")]
    public void Should_Reject_Invalid_Values(string? value)
    {
        Assert.False(Barcode.IsValid(value));
    }
}