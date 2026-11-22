using System.Text.Json;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;

namespace Reki.Infrastructure.WebPush;

internal sealed class WebPushNotificationSender(
    RekiPushServiceClient push,
    IOptions<JsonSerializerOptions> json,
    ILogger<WebPushNotificationSender> logger,
    TimeProvider timeProvider)
    : NotificationSender(NotificationChannelType.WebPush, logger, timeProvider)
{
    protected override async Task<NotificationStatus> SendInternalAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct)
    {
        var webPushChannel = (WebPushNotificationChannel)channel;
        var statuses = await Task.WhenAll(webPushChannel.Subscriptions.Values.Select(subscription =>
            SendDestinationAsync(notification, () => SendSubscriptionAsync(subscription, notification, ct), ct)));
        return statuses.Contains(NotificationStatus.Sent) ? NotificationStatus.Sent : NotificationStatus.Failed;
    }

    private Task SendSubscriptionAsync(WebPushSubscription destination, Notification notification, CancellationToken ct)
    {
        var subscription = new PushSubscription { Endpoint = destination.Endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, destination.P256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, destination.Auth);
        var payload = JsonSerializer.Serialize(
            new Dictionary<string, string>
            {
                ["title"] = "Реки",
                ["body"] = notification.Content,
                ["url"] = $"/?notification={notification.Id.Value:N}&notification_channel=webpush",
            },
            json.Value);
        return push.RequestPushMessageDeliveryAsync(subscription, new PushMessage(payload), ct);
    }
}
