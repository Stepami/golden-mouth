using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Application.Notifications;

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
        if (cache.TryGetValue(command.Player.Id, out DateTimeOffset _))
        {
            return Unit.Value;
        }

        var now = timeProvider.GetUtcNow();
        var player = command.Player;
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await players.ReplacePlayerAsync(player with { LastSeenAt = now }, token);
                if (now - player.LastSeenAt >= TimeSpan.FromHours(2) && player.NotificationChannels.Count > 0)
                {
                    var pending = await notifications.GetPendingAsync(player.Id, token);
                    if (pending is null)
                    {
                        await notifications.InsertAsync(NotificationSchedule.Create(player.Id, now), token);
                    }
                    else
                    {
                        await notifications.ReplaceAsync(pending with { SendAt = now.AddDays(1) }, token);
                    }
                }

                return true;
            },
            cancellationToken);
        cache.Set(player.Id, now, TimeSpan.FromMinutes(1));
        return Unit.Value;
    }
}