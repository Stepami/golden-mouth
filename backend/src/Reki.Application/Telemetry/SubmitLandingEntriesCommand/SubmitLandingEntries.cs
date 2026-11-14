using Mediator;

namespace Reki.Application.Telemetry.SubmitLandingEntriesCommand;

public sealed record SubmitLandingEntries(IReadOnlyList<LandingEntryEvent> Events) : ICommand;
