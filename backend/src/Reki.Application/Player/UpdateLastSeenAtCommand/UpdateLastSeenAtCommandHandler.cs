using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;

namespace Reki.Application.Player.UpdateLastSeenAtCommand;

internal sealed class UpdateLastSeenAtCommandHandler(
    IPlayerRepository players,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
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
                if (player.NotificationChannels.Count <= 0)
                {
                    return Unit.Value;
                }

                var sendAt = player.LastSeenAt.AddDays(1);
                var pending = await notifications.GetPendingAsync(player.Id, token);
                if (pending is null)
                {
                    await notifications.SaveAsync(Notification.New(player.Id, sendAt), token);
                }
                else if (player.LastSeenAt - oldLastSeenAt >= TimeSpan.FromHours(2))
                {
                    pending.Reschedule(sendAt);
                    await notifications.SaveAsync(pending, token);
                }

                return Unit.Value;
            },
            _ => cache.Set(key, now, TimeSpan.FromMinutes(5)),
            cancellationToken);
        return Unit.Value;
    }
}
