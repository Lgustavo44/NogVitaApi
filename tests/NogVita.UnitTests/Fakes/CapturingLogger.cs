using Microsoft.Extensions.Logging;

namespace NogVita.UnitTests.Fakes;

// Guarda cada linha de log, para os testes conferirem o que foi registrado (e o que não foi).
public sealed class CapturingLogger<T> : ILogger<T>
{
    public sealed record Entry(LogLevel Level, EventId EventId, string Message, IReadOnlyDictionary<string, object?> Values);

    public List<Entry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var values = state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? pairs.ToDictionary(p => p.Key, p => p.Value)
            : new Dictionary<string, object?>();

        Entries.Add(new Entry(logLevel, eventId, formatter(state, exception), values));
    }
}
