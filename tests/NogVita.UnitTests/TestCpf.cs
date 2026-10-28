namespace NogVita.UnitTests;

// Gera CPFs válidos e únicos para os testes (D176): cada chamada devolve um CPF diferente,
// com os dígitos verificadores certos. Assim, dois usuários de um mesmo teste nunca colidem,
// e nenhum teste precisa de uma lista de CPFs válidos copiada de outro arquivo.
public static class TestCpf
{
    // Base de 9 dígitos. Começa em 100.000.000 para nunca cair num CPF de dígitos todos iguais (inválido).
    private static int _next = 100_000_000;

    public static string Next()
    {
        var baseDigits = Interlocked.Increment(ref _next).ToString("D9");
        return WithCheckDigits(baseDigits);
    }

    // O cálculo oficial: cada dígito verificador vem da soma ponderada dos anteriores, módulo 11.
    public static string WithCheckDigits(string nineDigits)
    {
        if (nineDigits.Length != 9 || !nineDigits.All(char.IsAsciiDigit))
            throw new ArgumentException("A base do CPF precisa ter 9 dígitos.", nameof(nineDigits));

        var first = CheckDigit(nineDigits);
        var second = CheckDigit(nineDigits + first);
        return $"{nineDigits}{first}{second}";
    }

    private static int CheckDigit(string digits)
    {
        var weight = digits.Length + 1;
        var sum = digits.Sum(d => (d - '0') * weight--);
        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }
}
