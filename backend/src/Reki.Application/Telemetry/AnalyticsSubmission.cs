using Reki.Domain.Telemetry;

namespace Reki.Application.Telemetry;

public sealed record AnalyticsSubmission(
    SessionId Session,
    string Platform,
    IReadOnlyDictionary<string, string> Experiments,
    IReadOnlyList<AnalyticsEntry> Events)
{
    public string? LaunchSurface { get; init; }
}
