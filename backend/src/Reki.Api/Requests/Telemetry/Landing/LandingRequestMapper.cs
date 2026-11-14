using Reki.Application.Telemetry;
using Reki.Application.Telemetry.LinkLandingProfileCommand;
using Reki.Application.Telemetry.SubmitLandingEntriesCommand;

namespace Reki.Api.Requests.Telemetry.Landing;

internal static class LandingRequestMapper
{
    public static LandingVisit ToVisit(this LandingVisitRequest request) => new(
        Guid.Parse(request.VisitorId),
        Guid.Parse(request.EntryId),
        request.Variant,
        request.InitialVariant,
        request.Source,
        request.Switched,
        request.Campaign,
        request.Medium,
        request.TrafficSource,
        request.Content);

    public static SubmitLandingEntries ToCommand(this LandingEntryBatchRequest request) => new(
        request.Events.Select(entry => new LandingEntryEvent(
            Guid.Parse(entry.EventId), entry.Name, entry.Timestamp, entry.Visit.ToVisit(), entry.PostId)).ToArray());

    public static LinkLandingProfile ToLinkCommand(this LandingVisitRequest request) => new(request.ToVisit());
}
