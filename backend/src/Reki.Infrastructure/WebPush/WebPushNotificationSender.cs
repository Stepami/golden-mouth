using System.Net;
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
        var deliveries = webPushChannel.Subscriptions.Values.ToDictionary(
            destination => destination.Endpoint,
            destination => SendSubscriptionAsync(destination, payload, ct),
            StringComparer.Ordinal);
        try
        {
            await Task.WhenAll(deliveries.Values);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            foreach (var (endpoint, delivery) in deliveries)
            {
                if (delivery.Exception?.InnerException is PushServiceClientException { StatusCode: HttpStatusCode.Gone })
                {
                    webPushChannel.RemoveSubscription(endpoint);
                }
            }

            if (!deliveries.Values.Any(delivery => delivery.IsCompletedSuccessfully))
            {
                throw;
            }
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
