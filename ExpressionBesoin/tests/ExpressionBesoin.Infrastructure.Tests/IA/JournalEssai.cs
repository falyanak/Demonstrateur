using Microsoft.Extensions.Logging;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>Journal d'essai : garde le niveau et le message mis en forme de chaque écriture.</summary>
internal sealed class JournalEssai : ILogger
{
    public List<(LogLevel Niveau, string Message)> Ecritures { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Ecritures.Add((logLevel, formatter(state, exception)));
}
