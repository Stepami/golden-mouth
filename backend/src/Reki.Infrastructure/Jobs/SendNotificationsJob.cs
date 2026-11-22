using Microsoft.Extensions.DependencyInjection;
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
    IServiceProvider services,
    TimeProvider timeProvider) : ITickerFunction<NotificationChannelType>
{
    public async Task ExecuteAsync(TickerFunctionContext<NotificationChannelType> context, CancellationToken cancellationToken = default)
    {
        var type = context.Request;
        await foreach (var notification in notifications.GetDueAsync(type, cancellationToken))
        {
            var player = await players.GetPlayerAsync(notification.PlayerId, cancellationToken);
            if (player is null || !player.NotificationChannels.TryGetValue(type, out var channel))
            {
                await notifications.DeleteAsync(notification.Id, cancellationToken);
                continue;
            }

            var sender = services.GetRequiredKeyedService<INotificationSender>(type);
            var status = await sender.SendAsync(channel, notification, cancellationToken);
            notification.Complete(status);

            await unitOfWork.ExecuteAsync(
                async token =>
                {
                    await notifications.SaveAsync(notification, token);
                    if (status == NotificationStatus.Failed)
                    {
                        var recent = await notifications.GetLastThreeAsync(notification.PlayerId, type, token);
                        if (recent.Count == 3 && recent.All(item => item.Status == NotificationStatus.Failed))
                        {
                            player.RemoveNotificationChannel(type);
                            await players.ReplacePlayerAsync(player, token);
                            return status;
                        }
                    }

                    if (type == NotificationChannelType.WebPush)
                    {
                        await players.ReplacePlayerAsync(player, token);
                    }

                    if (await notifications.GetPendingAsync(notification.PlayerId, type, token) is null)
                    {
                        var next = Notification.New(notification.PlayerId, type, timeProvider.GetUtcNow().AddDays(1), notification.Content);
                        await notifications.SaveAsync(next, token);
                    }

                    return status;
                },
                cancellationToken);
        }
    }
}
