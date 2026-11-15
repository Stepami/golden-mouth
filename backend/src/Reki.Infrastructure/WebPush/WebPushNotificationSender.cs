using System.Text.Json;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Infrastructure.WebPush;

internal sealed class WebPushNotificationSender(
    RekiPushServiceClient push,
    IOptions<JsonSerializerOptions> json,
    ILogger<WebPushNotificationSender> logger,
    TimeProvider timeProvider)
    : NotificationSender(NotificationChannelType.WebPush, logger, timeProvider)
{
    protected override async Task SendInternalAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct)
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
    }
}
