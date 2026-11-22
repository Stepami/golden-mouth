namespace Reki.Domain.Notifications;

public abstract record PlayerNotificationChannel
{
    protected PlayerNotificationChannel(NotificationChannelType type) => Type = type;

    public NotificationChannelType Type { get; }
}
