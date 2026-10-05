namespace Reki.Domain.Telemetry;

public sealed record AnalyticsEntry(
    string Name,
    long Timestamp,
    string Screen,
    string? PreviousScreen,
    IReadOnlyDictionary<string, string> Properties);

