using Reki.Application;
using Reki.Application.Notifications;
using Reki.Application.Player;
using Reki.Domain.Notifications;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class SendNotificationsJob(
    INotificationRepository notifications,
    IPlayerRepository players,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IEnumerable<INotificationSender> senders) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        await foreach (var notification in notifications.GetDueAsync(cancellationToken))
        {
            var player = await players.GetPlayerAsync(notification.PlayerId, cancellationToken);
            if (player is null || player.NotificationChannels.Count == 0)
            {
                notification.MarkSent();
                await notifications.SaveAsync(notification, cancellationToken);
                continue;
            }

            await Task.WhenAll(
                player.NotificationChannels
                    .SelectMany(channel =>
                        senders.Select(sender =>
                            sender.SendAsync(channel, notification, cancellationToken))));

            var now = timeProvider.GetUtcNow();
            await unitOfWork.ExecuteAsync(
                async token =>
                {
                    notification.MarkSent();
                    await notifications.SaveAsync(notification, token);
                    await notifications.SaveAsync(
                        Notification.New(notification.PlayerId, now.AddDays(1), notification.Content),
                        token);
                    return true;
                },
                cancellationToken);
        }
    }
}
