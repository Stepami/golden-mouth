using System.Collections.ObjectModel;

namespace Reki.Domain.Notifications;

public sealed record WebPushNotificationChannel : PlayerNotificationChannel
{
    public WebPushNotificationChannel(IReadOnlyDictionary<string, WebPushSubscription> subscriptions)
        : base(NotificationChannelType.WebPush)
    {
        Subscriptions = new ReadOnlyDictionary<string, WebPushSubscription>(
            subscriptions.Values.ToDictionary(item => item.Endpoint, StringComparer.Ordinal));
    }

    public IReadOnlyDictionary<string, WebPushSubscription> Subscriptions { get; }
}
