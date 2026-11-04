using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NogVita.Infrastructure.Persistence.Configurations;

// D-V2-32: um valor gravado inteiro numa coluna jsonb. Serve quando ninguém filtra dentro dele (as perguntas
// e as respostas da anamnese). O comparador compara o JSON, para o EF notar uma lista trocada.
internal static class JsonColumn
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static PropertyBuilder<T> IsJson<T>(this PropertyBuilder<T> property) =>
        property
            .HasConversion(
                value => JsonSerializer.Serialize(value, Options),
                json => JsonSerializer.Deserialize<T>(json, Options)!,
                new ValueComparer<T>(
                    (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
                    value => JsonSerializer.Serialize(value, Options).GetHashCode(),
                    value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!))
            .HasColumnType("jsonb");
}
