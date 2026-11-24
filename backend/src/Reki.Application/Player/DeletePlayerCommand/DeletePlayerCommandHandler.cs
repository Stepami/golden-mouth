using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.Auth;
using Reki.Application.Notifications;
using Reki.Application.Player.Streaks;
using Reki.Application.Posts;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;

namespace Reki.Application.Player.DeletePlayerCommand;

internal sealed class DeletePlayerCommandHandler(
    IUnitOfWork unitOfWork,
    IPlayerRepository players,
    IPlayerStreakRepository streaks,
    IServiceScopeFactory scopeFactory,
    IWorldRepository worlds,
    IWorldPlayerCache worldCache,
    IPostRepository posts,
    INotificationRepository notifications,
    IBonusRepository bonuses,
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
                await posts.DeletePostsAsync(playerId, [], token);
                await worlds.DeleteWorldsAsync(playerId, [], token);
                await notifications.DeleteForPlayerAsync(playerId, token);
                await streaks.DeleteForPlayerAsync(playerId, token);
                await bonuses.DeleteForPlayerAsync(playerId, token);
                await players.DeletePlayerAsync(playerId, token);
                return Unit.Value;
            },
            _ => worldCache.ClearPlayer(playerId),
            cancellationToken);
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAuthSessionService>()
            .DeletePlayerSessionsAsync(playerId, cancellationToken);
        telemetry.Track(playerId, "player_deleted", new Dictionary<string, string>());
        return Unit.Value;
    }
}
