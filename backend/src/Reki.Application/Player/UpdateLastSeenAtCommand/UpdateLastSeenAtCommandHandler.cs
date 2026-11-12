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
        if (!command.Force && cache.TryGetValue(command.Player.Id, out DateTimeOffset _))
        {
            return Unit.Value;
        }

        var now = timeProvider.GetUtcNow();
        var oldLastSeenAt = command.Player.LastSeenAt;
        var player = command.Player with { LastSeenAt = now };
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await players.ReplacePlayerAsync(player, token);
                if (player.NotificationChannels.Count <= 0)
                {
                    return Unit.Value;
                }

                var sendAt = player.LastSeenAt.AddDays(1);
                await notifications.UpsertPendingAsync(
                    Notification.New(player.Id, sendAt),
                    player.LastSeenAt - oldLastSeenAt >= TimeSpan.FromHours(2),
                    token);

                return Unit.Value;
            },
            cancellationToken);
        cache.Set(player.Id, now, TimeSpan.FromMinutes(1));
        return Unit.Value;
    }
}