using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NogVita.Infrastructure.Foods.OpenFoodFacts;

// D171: o Open Food Facts às vezes manda um nutriente como "" (ou um texto que não é número).
// Sem este conversor, um único campo assim lançava JsonException e o produto inteiro se perdia.
// Agora o campo vira null, que é "não informado": o resto do produto continua valendo.
internal sealed class LenientDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.String => decimal.TryParse(
                reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : null,
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Valor inesperado para um número: {reader.TokenType}.")
        };

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteNumberValue(value.Value);
    }
}
