using Reki.Contracts.Telemetry;
using Reki.Domain.Telemetry;

namespace Reki.Application.Telemetry;

public sealed record AnalyticsSubmission(
    SessionId Session,
    string Platform,
    LaunchSurface LaunchSurface,
    IReadOnlyDictionary<string, string> Experiments,
    IReadOnlyList<AnalyticsEntry> Events);
