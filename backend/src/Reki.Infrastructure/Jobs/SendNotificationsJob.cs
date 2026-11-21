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
    public async Task PrepareAsync(CancellationToken cancellationToken)
    {
        await foreach (var notification in notifications.GetDueAsync(cancellationToken))
        {
            await unitOfWork.ExecuteAsync(
                async token =>
                {
                    var player = await players.GetPlayerAsync(notification.PlayerId, token);
                    var types = player?.NotificationChannels.Select(channel => channel.Type).Distinct().ToArray() ?? [];
                    var prepared = await notifications.PrepareDeliveriesAsync(notification, types, token);
                    if (prepared && types.Length > 0)
                    {
                        var next = Notification.New(notification.PlayerId, timeProvider.GetUtcNow().AddDays(1), notification.Content);
                        await notifications.SaveAsync(next, token);
                    }

                    return prepared;
                },
                cancellationToken);
        }
    }

    public async Task ExecuteAsync(TickerFunctionContext<NotificationChannelType> context, CancellationToken cancellationToken = default)
    {
        var type = context.Request;
        await foreach (var notification in notifications.GetDueDeliveriesAsync(type, cancellationToken))
        {
            var player = await players.GetPlayerAsync(notification.PlayerId, cancellationToken);
            var channels = player?.NotificationChannels.Where(channel => channel.Type == type).ToArray();
            if (channels is not { Length: > 0 })
            {
                await notifications.DeleteDeliveryAsync(notification.Id, type, cancellationToken);
                continue;
            }

            var sender = services.GetRequiredKeyedService<INotificationSender>(type);
            var results = await Task.WhenAll(channels.Select(channel => sender.SendAsync(channel, notification, cancellationToken)));
            var status = results.Contains(NotificationStatus.Sent) ? NotificationStatus.Sent : NotificationStatus.Failed;
            await unitOfWork.ExecuteAsync(
                async token =>
                {
                    await notifications.SetStatusAsync(notification.Id, type, status, token);
                    if (status == NotificationStatus.Failed)
                    {
                        var recent = await notifications.GetLastThreeAsync(notification.PlayerId, type, token);
                        if (recent.Count == 3 && recent.All(item => item == NotificationStatus.Failed))
                        {
                            await players.RemoveNotificationChannelsAsync(notification.PlayerId, channels, token);
                        }
                    }

                    return status;
                },
                cancellationToken);
        }
    }
}
