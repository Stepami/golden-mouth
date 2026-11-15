using System.Text.Json;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Infrastructure.WebPush;

internal sealed partial class WebPushNotificationSender(
    RekiPushServiceClient push,
    IOptions<JsonSerializerOptions> json,
    ILogger<WebPushNotificationSender> logger,
    TimeProvider timeProvider)
    : INotificationSender
{
    public async Task SendAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct)
    {
        if (channel.Type is not NotificationChannelType.WebPush)
        {
            return;
        }

        try
        {
            var subscription = new PushSubscription { Endpoint = channel.Properties["endpoint"] };
            subscription.SetKey(PushEncryptionKeyName.P256DH, channel.Properties["p256dh"]);
            subscription.SetKey(PushEncryptionKeyName.Auth, channel.Properties["auth"]);
            var payload = JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    ["title"] = "Реки",
                    ["body"] = notification.Content,
                    ["url"] = $"/?notification={notification.Id.Value:N}&notification_channel=webpush",
                },
                json.Value);
            await push.RequestPushMessageDeliveryAsync(subscription, new PushMessage(payload), ct);

            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["reki.stream"] = "product",
                ["event_name"] = "notification_sent",
                ["activity_kind"] = "delivery",
                ["player_id"] = notification.PlayerId.Value.ToString("N"),
                ["prop_id"] = notification.Id.Value.ToString("N"),
                ["prop_kind"] = "webpush",
                ["event_time_unix_ms"] = timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
            });
            LogSent();
        }
        catch (Exception error)
        {
            LogSendError(error);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification accepted by provider")]
    private partial void LogSent();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification delivery failed for channel WebPush")]
    private partial void LogSendError(Exception ex);
}