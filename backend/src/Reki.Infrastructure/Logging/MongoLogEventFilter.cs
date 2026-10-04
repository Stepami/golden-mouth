using Serilog.Events;

namespace Reki.Infrastructure.Logging;

internal static class MongoLogEventFilter
{
    private const string ApplicationSourcePrefix = "Reki.";
    private const string BusinessEventNamePrefix = "Business.";

    public static bool ShouldPersist(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        return IsBusinessEvent(logEvent) || IsApplicationError(logEvent);
    }

    private static bool IsBusinessEvent(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("EventId", out LogEventPropertyValue? eventId) ||
            eventId is not StructureValue eventIdStructure)
        {
            return false;
        }

        LogEventProperty? eventNameProperty = eventIdStructure.Properties.FirstOrDefault(
            property => property.Name == "Name");

        return eventNameProperty?.Value is ScalarValue { Value: string eventName } &&
            eventName.StartsWith(BusinessEventNamePrefix, StringComparison.Ordinal);
    }

    private static bool IsApplicationError(LogEvent logEvent)
    {
        return logEvent.Level >= LogEventLevel.Error &&
            logEvent.Properties.TryGetValue(
                "SourceContext",
                out LogEventPropertyValue? sourceContext) &&
            sourceContext is ScalarValue { Value: string source } &&
            source.StartsWith(ApplicationSourcePrefix, StringComparison.Ordinal);
    }
}
