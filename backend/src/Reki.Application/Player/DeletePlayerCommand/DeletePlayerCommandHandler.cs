using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.Auth;
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
    IServiceScopeFactory scopeFactory,
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
        var playerId = rekiContextAccessor.Context.PlayerId;
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                var worldIds = await worlds.GetWorldIdsAsync(playerId, token);
                await posts.DeletePostsAsync(worldIds, token);
                await events.DeleteEventsAsync(playerId, token);
                await worlds.DeleteWorldsAsync(playerId, token);
                await invitations.DeleteInvitesAsync(playerId, token);
                await players.DeletePlayerAsync(playerId, token);
                return Unit.Value;
            },
            cancellationToken);
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAuthSessionService>().DeletePlayerSessionsAsync(playerId, cancellationToken);
        telemetry.Track(playerId, "player_deleted", new Dictionary<string, string>());
        return Unit.Value;
    }
}