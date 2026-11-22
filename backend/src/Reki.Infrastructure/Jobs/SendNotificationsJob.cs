using Microsoft.Extensions.DependencyInjection;
using Reki.Application;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Infrastructure.Mongo.Repositories;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class SendNotificationsJob(
    MongoNotificationRepository notifications,
    MongoPlayerRepository players,
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
            var channels = player?.NotificationChannels.Where(channel => channel.Type == type).ToArray();
            if (channels is not { Length: > 0 })
            {
                await notifications.DeleteAsync(notification.Id, cancellationToken);
                continue;
            }

            var sender = services.GetRequiredKeyedService<INotificationSender>(type);
            var results = await Task.WhenAll(channels.Select(channel => sender.SendAsync(channel, notification, cancellationToken)));
            var status = results.Contains(NotificationStatus.Sent) ? NotificationStatus.Sent : NotificationStatus.Failed;
            notification.Complete(status);

            await unitOfWork.ExecuteAsync(
                async token =>
                {
                    await notifications.SaveAsync(notification, token);
                    if (status == NotificationStatus.Failed)
                    {
                        var recent = await notifications.GetLastThreeAsync(notification.PlayerId, type, token);
                        if (recent.Count == 3 && recent.All(item => item == NotificationStatus.Failed))
                        {
                            await players.RemoveNotificationChannelsAsync(notification.PlayerId, channels, token);
                        }
                    }

                    var current = await players.GetPlayerAsync(notification.PlayerId, token);
                    if (current?.NotificationChannels.Any(channel => channel.Type == type) == true &&
                        await notifications.GetPendingAsync(notification.PlayerId, type, token) is null)
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
