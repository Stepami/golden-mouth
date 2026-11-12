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
    ILogger<WebPushNotificationSender> logger)
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
                    ["url"] = "/",
                },
                json.Value);
            await push.RequestPushMessageDeliveryAsync(subscription, new PushMessage(payload), ct);
        }
        catch (Exception error)
        {
            LogSendError(error);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification delivery failed for channel WebPush")]
    private partial void LogSendError(Exception ex);
}