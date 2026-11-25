using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Application.Player.UpdateLastSeenAtCommand;

internal sealed class UpdateLastSeenAtCommandHandler(
    IPlayerRepository players,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    INotificationContentFactory<PlayerProfile> notificationContentFactory,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateLastSeenAt>
{
    public async ValueTask<Unit> Handle(UpdateLastSeenAt command, CancellationToken cancellationToken)
    {
        var key = CacheKeyPrefixes.UpdateLastSeenAt + command.Player.Id;
        if (!command.Force && cache.TryGetValue(key, out DateTimeOffset _))
        {
            return Unit.Value;
        }

        var now = timeProvider.GetUtcNow();
        var oldLastSeenAt = command.Player.LastSeenAt;
        var player = command.Player.UpdateLastSeenAt(now);

        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await players.ReplacePlayerAsync(player, token);
                var sendAt = player.LastSeenAt.AddDays(1);
                foreach (var type in player.NotificationChannels.Keys)
                {
                    var pending = await notifications.GetPendingAsync(player.Id, type, token);
                    if (pending is null)
                    {
                        var content = await notificationContentFactory.GetContent(player);
                        await notifications.SaveAsync(Notification.New(player.Id, type, sendAt, content), token);
                    }
                    else if (player.LastSeenAt - oldLastSeenAt >= TimeSpan.FromHours(2))
                    {
                        var content = await notificationContentFactory.GetContent(player);
                        pending.Reschedule(sendAt, content);
                        await notifications.SaveAsync(pending, token);
                    }
                }

                return Unit.Value;
            },
            _ => cache.Set(key, now, TimeSpan.FromMinutes(5)),
            cancellationToken);
        return Unit.Value;
    }
}
