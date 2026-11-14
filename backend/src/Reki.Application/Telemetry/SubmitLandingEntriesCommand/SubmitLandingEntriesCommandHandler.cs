using Mediator;

namespace Reki.Application.Telemetry.SubmitLandingEntriesCommand;

internal sealed class SubmitLandingEntriesCommandHandler(LandingTelemetry telemetry)
    : ICommandHandler<SubmitLandingEntries>
{
    public ValueTask<Unit> Handle(SubmitLandingEntries command, CancellationToken cancellationToken)
    {
        foreach (var entry in command.Events)
        {
            telemetry.TrackEntry(entry);
        }

        return ValueTask.FromResult(Unit.Value);
    }
}
