using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.Auth;
using Reki.Application.Invites;
using Reki.Application.Notifications;
using Reki.Application.Posts;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;

namespace Reki.Application.Player.DeletePlayerCommand;

internal sealed class DeletePlayerCommandHandler(
    IUnitOfWork unitOfWork,
    IPlayerRepository players,
    IServiceScopeFactory scopeFactory,
    IWorldRepository worlds,
    IPostRepository posts,
    IInvitationRepository invitations,
    INotificationRepository notifications,
    ProductTelemetry telemetry)
    : ICommandHandler<DeletePlayer>
{
    public async ValueTask<Unit> Handle(
        DeletePlayer command,
        CancellationToken cancellationToken)
    {
        var playerId = command.PlayerId;
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                var worldIds = await worlds.GetWorldIdsAsync(playerId, token);
                await posts.DeletePostsAsync(worldIds, token);
                await worlds.DeleteWorldsAsync(worldIds, token);
                await invitations.DeleteInvitesAsync(playerId, token);
                await notifications.DeleteForPlayerAsync(playerId, token);
                await players.DeletePlayerAsync(playerId, token);
                return Unit.Value;
            },
            cancellationToken);
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAuthSessionService>()
            .DeletePlayerSessionsAsync(playerId, cancellationToken);
        telemetry.Track(playerId, "player_deleted", new Dictionary<string, string>());
        return Unit.Value;
    }
}
