using Mediator;
using Reki.Application.Events;
using Reki.Application.Invites;
using Reki.Application.Posts;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;

namespace Reki.Application.Player.DeletePlayerCommand;

internal sealed class DeletePlayerCommandHandler(
    IUnitOfWork unitOfWork,
    IPlayerRepository players,
    IWorldRepository worlds,
    IPostRepository posts,
    IDailyGameEventRepository events,
    IInvitationRepository invitations,
    IRekiContextAccessor rekiContextAccessor,
    ProductTelemetry telemetry)
    : ICommandHandler<DeletePlayer>
{
    public async ValueTask<Unit> Handle(
        DeletePlayer request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                var worldIds = await worlds.GetWorldIdsAsync(player.Id, token);
                await posts.DeletePostsAsync(worldIds, token);
                await events.DeleteEventsAsync(player.Id, token);
                await worlds.DeleteWorldsAsync(player.Id, token);
                await invitations.DeleteInvitesAsync(player.Id, token);
                await players.DeletePlayerAsync(player.Id, token);
                return Unit.Value;
            },
            cancellationToken);
        telemetry.Track(player.Id, "player_deleted", new Dictionary<string, string>());
        return Unit.Value;
    }
}