using NogVita.Domain.Common;

namespace NogVita.Domain.Users;

public sealed record Cpf
{
    public string Value { get; }

    private Cpf(string value) => Value = value;

    public static Cpf Create(string input)
    {
        var digits = Normalize(input);

        if (!IsValidDigits(digits))
        {
            throw new DomainException("CPF inválido.");
        }

        return new Cpf(digits);
    }

    public static bool IsValid(string input) => IsValidDigits(Normalize(input));

    public string Formatted =>  $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..11]}";

    public override string ToString() => Value;

    private static string Normalize(string input)
    {
        return new string(input.Where(char.IsAsciiDigit).ToArray());
    }

    private static bool IsValidDigits(string digits)
    {
        if (digits.Length != 11)
        {
            return false;
        }

        if (digits.All(d => d == digits[0]))
        {
            return false;
        }

        var firstVerifier = CalculateVerifier(digits[..9]);
        var secondVerifier = CalculateVerifier(digits[..10]);

        if (firstVerifier != digits[9] || secondVerifier != digits[10])
        {
            return false;
        }

        return true;
    }

    private static char CalculateVerifier(string v)
    {
        var sum = 0;
        var multiplier = 2;

        for (var i = v.Length - 1; i >= 0; i--)
        {
            sum += (v[i] - '0') * multiplier;
            multiplier++;

        }

        var remainder = sum % 11;
        return remainder < 2 ? '0' : (char)('0' + 11 - remainder);
    }
}