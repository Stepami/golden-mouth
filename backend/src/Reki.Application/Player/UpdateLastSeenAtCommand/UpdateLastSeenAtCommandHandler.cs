using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Application.Notifications;
using Reki.Application.Player.Streaks.UpdateStreakCommand;
using Reki.Domain.Notifications;

namespace Reki.Application.Player.UpdateLastSeenAtCommand;

internal sealed class UpdateLastSeenAtCommandHandler(
    IPlayerRepository players,
    INotificationRepository notifications,
    ISender sender,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateLastSeenAt>
{
    public async ValueTask<Unit> Handle(UpdateLastSeenAt command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (!command.Force && cache.TryGetValue(command.Player.Id, out DateTimeOffset lastUpdated) &&
            lastUpdated.UtcDateTime.Date == now.UtcDateTime.Date)
        {
            return Unit.Value;
        }

        var oldLastSeenAt = command.Player.LastSeenAt;
        var player = command.Player.UpdateLastSeenAt(now);
        var streakChanged = await unitOfWork.ExecuteAsync(
            async token =>
            {
                var changed = await sender.Send(new UpdateStreak(player.Id, now), token);
                await players.ReplacePlayerAsync(player, token);
                if (player.NotificationChannels.Count <= 0)
                {
                    return changed;
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

                return changed;
            },
            cancellationToken);
        if (streakChanged)
        {
            cache.Remove(CacheKeyPrefixes.PlayerStreak + command.Player.Id);
        }

        cache.Set(command.Player.Id, now, TimeSpan.FromMinutes(1));

        return Unit.Value;
    }
}
