using Mediator;
using Reki.Application.Common;
using Reki.Application.Telemetry;

namespace Reki.Application.Player;

public sealed class DeletePlayerHandler(
    IGameStore store,
    ISender sender,
    ProductTelemetry telemetry)
    : IRequestHandler<DeletePlayerCommand>
{
    public async ValueTask<Unit> Handle(
        DeletePlayerCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        await store.DeletePlayerAsync(player, cancellationToken);
        telemetry.Track(player.Id, "player_deleted", new Dictionary<string, string>());
        return Unit.Value;
    }
}

