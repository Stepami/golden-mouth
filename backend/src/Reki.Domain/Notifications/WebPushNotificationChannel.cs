using System.Collections.ObjectModel;

namespace Reki.Domain.Notifications;

public sealed record WebPushNotificationChannel : PlayerNotificationChannel
{
    private readonly Dictionary<string, WebPushSubscription> _subscriptions;

    public WebPushNotificationChannel(IReadOnlyDictionary<string, WebPushSubscription> subscriptions)
        : base(NotificationChannelType.WebPush)
    {
        _subscriptions = subscriptions.Values.ToDictionary(item => item.Endpoint, StringComparer.Ordinal);
        Subscriptions = new ReadOnlyDictionary<string, WebPushSubscription>(_subscriptions);
    }

    public IReadOnlyDictionary<string, WebPushSubscription> Subscriptions { get; }

    public void RemoveSubscription(string endpoint) => _subscriptions.Remove(endpoint);
}
