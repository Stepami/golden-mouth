namespace Reki.Domain.Telemetry;

// WorldId отсутствует у событий до создания мира.
public sealed record AnalyticsEntry(
    Worlds.WorldId? WorldId,
    string Name,
    long Timestamp,
    string Screen,
    string? PreviousScreen,
    IReadOnlyDictionary<string, string> Properties);