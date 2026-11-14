using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.RekiContext;

namespace Reki.Application.Telemetry.SubmitAnalyticsCommand;

internal sealed class SubmitAnalyticsCommandHandler(
    ProductTelemetry telemetry,
    IRekiContextAccessor rekiContextAccessor)
    : ICommandHandler<SubmitAnalytics>
{
    public async ValueTask<Unit> Handle(
        SubmitAnalytics command,
        CancellationToken cancellationToken)
    {
        if (command.Anonymous)
        {
            telemetry.TrackAnonymousSubmission(command.Submission with
            {
                Events = command.Submission.Events.Where(entry => SubmitAnalytics.IsLandingEvent(entry.Name)).ToArray(),
            });
            return Unit.Value;
        }

        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        foreach (var worldId in command.Submission.Events.Select(entry => entry.WorldId).OfType<Domain.Worlds.WorldId>().Distinct())
        {
            _ = (await rekiContext.Worlds).GetValueOrDefault(worldId)
            ?? throw new ResourceNotFoundException("World not found.");
        }

        telemetry.TrackSubmission(player, command.Submission);
        return Unit.Value;
    }
}