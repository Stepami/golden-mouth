using Reki.Domain.Player;

namespace Reki.Domain.Telemetry;

public sealed record AnalyticsBatch(
    PlayerId PlayerId,
    SessionId Session,
    string Platform,
    IReadOnlyDictionary<string, string> Experiments,
    IReadOnlyList<AnalyticsEntry> Events,
    DateTimeOffset ReceivedAtUtc);
