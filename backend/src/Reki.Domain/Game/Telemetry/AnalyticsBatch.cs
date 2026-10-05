namespace Reki.Domain.Game;

public sealed record AnalyticsBatch(
    string PlayerId,
    string Session,
    string Platform,
    IReadOnlyDictionary<string, string> Experiments,
    IReadOnlyList<AnalyticsEntry> Events,
    DateTimeOffset ReceivedAtUtc);
