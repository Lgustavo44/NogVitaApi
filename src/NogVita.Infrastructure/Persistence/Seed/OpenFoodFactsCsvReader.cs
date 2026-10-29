using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace NogVita.Infrastructure.Persistence.Seed;

internal sealed record OpenFoodFactsCsvRow(
    string Code,
    string Name,
    string? Brand,
    decimal? EnergyKcal,
    decimal? Protein,
    decimal? Carbohydrate,
    decimal? Fat,
    decimal? Fiber,
    decimal? SodiumMg);

internal static class OpenFoodFactsCsvReader
{
    internal const string ResourceName = "NogVita.Infrastructure.Seed.off_brasil.csv";

    // D192: o SHA-256 do arquivo embutido, em hexadecimal. Muda só quando o CSV muda (uma nova versão da base).
    public static string ComputeHash()
    {
        using var stream = OpenResource();
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
    }

    // D151: devolve uma linha por vez (yield), sem carregar o arquivo inteiro na memória.
    public static IEnumerable<OpenFoodFactsCsvRow> ReadRows()
    {
        using var stream = OpenResource();

        using var parser = new TextFieldParser(stream, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true
        };
        parser.SetDelimiters(",");

        var header = parser.ReadFields() ?? throw new InvalidOperationException("O CSV do Open Food Facts está vazio.");

        int Column(string name) => Array.IndexOf(header, name) is var index and >= 0
            ? index
            : throw new InvalidOperationException($"A coluna '{name}' não foi encontrada no CSV do Open Food Facts.");

        var code = Column("code");
        var name = Column("name");
        var brand = Column("brand");
        var energy = Column("energy_kcal");
        var protein = Column("protein");
        var carbohydrate = Column("carbohydrate");
        var fat = Column("fat");
        var fiber = Column("fiber");
        var sodium = Column("sodium_mg");

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();

            if (fields is null)
            {
                continue;
            }

            yield return new OpenFoodFactsCsvRow(
                fields[code],
                fields[name],
                string.IsNullOrWhiteSpace(fields[brand]) ? null : fields[brand],
                ParseDecimal(fields[energy]),
                ParseDecimal(fields[protein]),
                ParseDecimal(fields[carbohydrate]),
                ParseDecimal(fields[fat]),
                ParseDecimal(fields[fiber]),
                ParseDecimal(fields[sodium]));
        }
    }

    private static decimal? ParseDecimal(string raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : decimal.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static Stream OpenResource() =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"O recurso embutido '{ResourceName}' não foi encontrado.");
}
