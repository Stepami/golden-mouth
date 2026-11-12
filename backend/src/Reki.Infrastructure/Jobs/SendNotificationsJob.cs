using System.Globalization;
using System.Text.Json;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application;
using Reki.Application.Notifications;
using Reki.Application.Player;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Telegram.Bot;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed partial class SendNotificationsJob(
    INotificationRepository notifications,
    IPlayerRepository players,
    IUnitOfWork unitOfWork,
    ITelegramBotClient telegram,
    PushServiceClient push,
    IOptions<JsonSerializerOptions> json,
    TimeProvider timeProvider,
    ILogger<SendNotificationsJob> logger) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        await foreach (var notification in notifications.GetDueAsync(timeProvider.GetUtcNow(), cancellationToken))
        {
            var player = await players.GetPlayerAsync(notification.PlayerId, cancellationToken);
            if (player is null || player.NotificationChannels.Count == 0)
            {
                await notifications.DeleteAsync(notification.Id, cancellationToken);
                continue;
            }

            await SendAsync(player, notification.Content, cancellationToken);

            var now = timeProvider.GetUtcNow();
            await unitOfWork.ExecuteAsync(
                async token =>
                {
                    await notifications.ReplaceAsync(notification with { Status = NotificationStatus.Sent }, token);
                    await notifications.InsertAsync(
                        NotificationSchedule.Create(notification.PlayerId, now) with { Content = notification.Content },
                        token);
                    return true;
                },
                cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification delivery failed for channel {ChannelType}")]
    private static partial void LogDeliveryFailed(ILogger logger, NotificationChannelType channelType);

    private async Task SendAsync(PlayerProfile player, string content, CancellationToken cancellationToken)
    {
        foreach (var channel in player.NotificationChannels)
        {
            try
            {
                switch (channel.Type)
                {
                    case NotificationChannelType.Telegram:
                    {
                        await telegram.SendMessage(
                            long.Parse(channel.Properties["chatId"], CultureInfo.InvariantCulture),
                            content,
                            cancellationToken: cancellationToken);
                        break;
                    }

                    case NotificationChannelType.WebPush:
                    {
                        var subscription = new PushSubscription { Endpoint = channel.Properties["endpoint"] };
                        subscription.SetKey(PushEncryptionKeyName.P256DH, channel.Properties["p256dh"]);
                        subscription.SetKey(PushEncryptionKeyName.Auth, channel.Properties["auth"]);
                        var payload = JsonSerializer.Serialize(
                            new Dictionary<string, string> { ["title"] = "Реки", ["body"] = content, ["url"] = "/" },
                            json.Value);
                        await push.RequestPushMessageDeliveryAsync(
                            subscription,
                            new PushMessage(payload),
                            cancellationToken);
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                LogDeliveryFailed(logger, channel.Type);
            }
        }
    }
}