namespace Reki.Application.Telemetry;

public sealed record LandingEntryEvent(Guid EventId, string Name, long Timestamp, LandingVisit Visit, Guid? PostId);
