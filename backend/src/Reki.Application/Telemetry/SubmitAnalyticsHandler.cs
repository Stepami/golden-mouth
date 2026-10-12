using Mediator;
using Reki.Application.Common;
using Reki.Application.Player;
using Reki.Application.Worlds;

namespace Reki.Application.Telemetry;

public sealed class SubmitAnalyticsHandler(
    ProductTelemetry telemetry,
    ISender sender,
    IGameStore store)
    : IRequestHandler<SubmitAnalyticsCommand>
{
    public async ValueTask<Unit> Handle(
        SubmitAnalyticsCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        foreach (var worldId in request.Submission.Events.Select(entry => entry.WorldId).OfType<Reki.Domain.Worlds.WorldId>().Distinct())
        {
            _ = await sender.Send(new GetWorldQuery(player.Id, worldId), cancellationToken);
        }

        var invite = await store.GetInviteAsync(player.Id, cancellationToken);
        telemetry.TrackSubmission(player, request.Submission, invite is not null);
        return Unit.Value;
    }
}