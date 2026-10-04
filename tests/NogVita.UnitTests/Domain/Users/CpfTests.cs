using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Domain.Users;

public class CpfTests
{
    [Fact]
    public void Should_Create_Cpf_From_Digits()
    {
        var cpf = Cpf.Create("52998224725");

        Assert.Equal("52998224725", cpf.Value);
    }

    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData(" 529 982 247 25 ")]
    [InlineData("529982247-25")]
    public void Should_Normalize_Formatted_Cpf(string input)
    {
        var cpf = Cpf.Create(input);

        Assert.Equal("52998224725", cpf.Value);
    }

    [Fact]
    public void Should_Reject_Invalid_Check_Digits()
    {
        Assert.Throws<DomainException>(() => Cpf.Create("52998224724"));
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999")]
    public void Should_Reject_Repeated_Digits(string input)
    {
        Assert.Throws<DomainException>(() => Cpf.Create(input));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("529982247250")]
    public void Should_Reject_Wrong_Length(string input)
    {
        Assert.Throws<DomainException>(() => Cpf.Create(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc.def.ghi-jk")]
    public void Should_Reject_Empty_Or_Without_Digits(string input)
    {
        Assert.Throws<DomainException>(() => Cpf.Create(input));
    }

    [Fact]
    public void Should_Be_Equal_When_Same_Digits()
    {
        var formatted = Cpf.Create("529.982.247-25");
        var digits = Cpf.Create("52998224725");

        Assert.Equal(formatted, digits);
    }

    [Fact]
    public void Should_Format_Cpf()
    {
        var cpf = Cpf.Create("52998224725");

        Assert.Equal("529.982.247-25", cpf.Formatted);
    }

    [Theory]
    [InlineData("52998224725", true)]
    [InlineData("529.982.247-25", true)]
    [InlineData("52998224724", false)]
    [InlineData("11111111111", false)]
    public void Should_Check_Validity_Without_Throwing(string input, bool expected)
    {
        Assert.Equal(expected, Cpf.IsValid(input));
    }
}