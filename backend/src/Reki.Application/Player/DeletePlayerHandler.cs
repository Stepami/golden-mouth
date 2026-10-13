using Mediator;
using Reki.Application.Common;
using Reki.Application.Telemetry;

namespace Reki.Application.Player;

public sealed class DeletePlayerHandler(
    IGameStore store,
    IRekiContextAccessor contextAccessor,
    ProductTelemetry telemetry)
    : IRequestHandler<DeletePlayerCommand>
{
    public async ValueTask<Unit> Handle(
        DeletePlayerCommand request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        await store.DeletePlayerAsync(player, cancellationToken);
        telemetry.Track(player.Id, "player_deleted", new Dictionary<string, string>());
        return Unit.Value;
    }
}

