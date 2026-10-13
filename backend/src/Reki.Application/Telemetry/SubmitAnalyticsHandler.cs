using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;

namespace Reki.Application.Telemetry;

public sealed class SubmitAnalyticsHandler(
    ProductTelemetry telemetry,
    IRekiContextAccessor contextAccessor)
    : IRequestHandler<SubmitAnalyticsCommand>
{
    public async ValueTask<Unit> Handle(
        SubmitAnalyticsCommand request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        foreach (var worldId in request.Submission.Events.Select(entry => entry.WorldId).OfType<Reki.Domain.Worlds.WorldId>().Distinct())
        {
            _ = (await rekiContext.Worlds).GetValueOrDefault(worldId)
            ?? throw new ResourceNotFoundException("World not found.");
        }

        telemetry.TrackSubmission(player, request.Submission);
        return Unit.Value;
    }
}