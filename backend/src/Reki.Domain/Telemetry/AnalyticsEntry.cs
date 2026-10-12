namespace Reki.Domain.Telemetry;

/// <summary>
/// WorldId отсутствует у событий до создания мира.
/// </summary>
public sealed record AnalyticsEntry(
    Worlds.WorldId? WorldId,
    string Name,
    long Timestamp,
    string Screen,
    string? PreviousScreen,
    IReadOnlyDictionary<string, string> Properties);