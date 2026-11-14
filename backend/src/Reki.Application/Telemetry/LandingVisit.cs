namespace Reki.Application.Telemetry;

public sealed record LandingVisit(
    Guid VisitorId,
    Guid EntryId,
    string Variant,
    string InitialVariant,
    string Source,
    bool Switched,
    string? Campaign,
    string? Medium,
    string? TrafficSource,
    string? Content);
