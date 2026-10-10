namespace NogVita.Domain.Foods;

public static class Barcode
{
    private static readonly int[] ValidLengths = [8, 12, 13, 14];

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        if (!value.All(char.IsAsciiDigit))
            return false;

        if (!ValidLengths.Contains(value.Length))
            return false;

        return true;
    }
}