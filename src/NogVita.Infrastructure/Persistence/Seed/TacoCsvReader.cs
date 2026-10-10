using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using NogVita.Domain.Foods;

namespace NogVita.Infrastructure.Persistence.Seed;

internal static class TacoCsvReader
{
    internal const string ResourceName = "NogVita.Infrastructure.Seed.taco_composicao.csv";
    internal const decimal TraceThreshold = 0.0001m;

    public static IReadOnlyList<Food> ReadFoods()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"O recurso embutido '{ResourceName}' não foi encontrado.");

        using var parser = new TextFieldParser(stream, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true
        };
        parser.SetDelimiters(",");

        var header = parser.ReadFields()
            ?? throw new InvalidOperationException("O CSV da TACO está vazio.");

        var number = ColumnIndex(header, "numero_alimento");
        var description = ColumnIndex(header, "descricao");
        var category = ColumnIndex(header, "categoria");
        var energy = ColumnIndex(header, "energia_kcal");
        var protein = ColumnIndex(header, "proteina_g");
        var carbohydrate = ColumnIndex(header, "carboidrato_g");
        var fat = ColumnIndex(header, "lipideos_g");
        var fiber = ColumnIndex(header, "fibra_g");
        var sodium = ColumnIndex(header, "sodio_mg");

        var foods = new List<Food>();

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();

            if (fields is null)
            {
                continue;
            }

            var nutrients = new NutrientsPer100g(
                ParseNutrient(fields[energy]),
                ParseNutrient(fields[protein]),
                ParseNutrient(fields[carbohydrate]),
                ParseNutrient(fields[fat]),
                ParseNutrient(fields[fiber]),
                ParseNutrient(fields[sodium]));

            foods.Add(Food.FromTaco(
                int.Parse(fields[number], CultureInfo.InvariantCulture),
                fields[description],
                fields[category],
                nutrients));
        }

        return foods;
    }

    internal static decimal? ParseNutrient(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }
        var valor = decimal.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (valor < TraceThreshold)
        {
            return 0m;
        }
        return Math.Round(valor, 4);
    }

    private static int ColumnIndex(string[] header, string column)
    {
        var index = Array.IndexOf(header, column);

        return index >= 0
            ? index
            : throw new InvalidOperationException($"A coluna '{column}' não foi encontrada no CSV da TACO.");
    }
}