using NogVita.Domain.Users;

namespace NogVita.UnitTests;

// O gerador precisa ser confiável: um CPF inválido faria qualquer teste que o usa quebrar por um motivo errado.
public class TestCpfTests
{
    [Fact]
    public void Generated_Cpfs_Should_Be_Accepted_By_The_Domain()
    {
        for (var i = 0; i < 200; i++)
        {
            var cpf = TestCpf.Next();
            Assert.Equal(cpf, Cpf.Create(cpf).Value);
        }
    }

    [Fact]
    public void Generated_Cpfs_Should_Be_Unique()
    {
        var cpfs = Enumerable.Range(0, 200).Select(_ => TestCpf.Next()).ToList();

        Assert.Equal(cpfs.Count, cpfs.Distinct().Count());
    }

    // Os mesmos CPFs válidos que os testes do Domain usam: o cálculo bate com o oficial.
    [Theory]
    [InlineData("123456789", "12345678909")]
    [InlineData("111444777", "11144477735")]
    [InlineData("529982247", "52998224725")]
    public void Should_Calculate_The_Official_Check_Digits(string baseDigits, string expected)
    {
        Assert.Equal(expected, TestCpf.WithCheckDigits(baseDigits));
    }
}
