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
    protected override async Task SendInternalAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct)
    {
        var webPushChannel = (WebPushNotificationChannel)channel;
        if (webPushChannel.Subscriptions.Count == 0)
        {
            throw new InvalidOperationException("Web Push channel has no subscriptions.");
        }

        var payload = JsonSerializer.Serialize(
            new Dictionary<string, string>
            {
                ["title"] = "Реки",
                ["body"] = notification.Content,
                ["url"] = $"/?notification={notification.Id.Value:N}&notification_channel=webpush",
            },
            json.Value);
        var deliveries = webPushChannel.Subscriptions.Values.Select(destination => SendSubscriptionAsync(destination, payload, ct)).ToArray();
        try
        {
            await Task.WhenAll(deliveries);
        }
        catch (Exception) when (!ct.IsCancellationRequested && deliveries.Any(delivery => delivery.IsCompletedSuccessfully))
        {
            // One accepted subscription is enough to mark this notification as sent.
        }
    }

    private async Task SendSubscriptionAsync(WebPushSubscription destination, string payload, CancellationToken ct)
    {
        var subscription = new PushSubscription { Endpoint = destination.Endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, destination.P256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, destination.Auth);
        await push.RequestPushMessageDeliveryAsync(subscription, new PushMessage(payload), ct);
    }
}
