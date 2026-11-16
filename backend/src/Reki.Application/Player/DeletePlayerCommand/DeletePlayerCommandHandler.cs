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
    IWorldPlayerCache worldCache,
    IPostRepository posts,
    IInvitationService invitations,
    IInvitationRepository invitationRepository,
    INotificationRepository notifications,
    ProductTelemetry telemetry)
    : ICommandHandler<DeletePlayer>
{
    public async ValueTask<Unit> Handle(
        DeletePlayer command,
        CancellationToken cancellationToken)
    {
        var playerId = command.PlayerId;
        var invitees = await unitOfWork.ExecuteAsync(
            async token =>
            {
                await posts.DeletePostsAsync(playerId, [], token);
                await worlds.DeleteWorldsAsync(playerId, [], token);
                var affected = await invitationRepository.DeleteInvitesAsync(playerId, token);
                await notifications.DeleteForPlayerAsync(playerId, token);
                await players.DeletePlayerAsync(playerId, token);
                return affected;
            },
            cancellationToken);
        worldCache.ClearPlayer(playerId);
        invitations.ClearPlayer(playerId);
        foreach (var invitee in invitees)
        {
            invitations.ClearPlayer(invitee);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAuthSessionService>()
            .DeletePlayerSessionsAsync(playerId, cancellationToken);
        telemetry.Track(playerId, "player_deleted", new Dictionary<string, string>());
        return Unit.Value;
    }
}
