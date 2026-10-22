using Mediator;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;

namespace Reki.Application.Player.DeletePlayerCommand;

internal sealed class DeletePlayerCommandHandler(
    IGameStore store,
    IRekiContextAccessor rekiContextAccessor,
    ProductTelemetry telemetry)
    : ICommandHandler<DeletePlayer>
{
    public async ValueTask<Unit> Handle(
        DeletePlayer request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        await store.DeletePlayerAsync(player, cancellationToken);
        telemetry.Track(player.Id, "player_deleted", new Dictionary<string, string>());
        return Unit.Value;
    }
}

