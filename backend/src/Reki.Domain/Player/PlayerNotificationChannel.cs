namespace Reki.Domain.Player;

public sealed record PlayerNotificationChannel(
    NotificationChannelType Type,
    IReadOnlyDictionary<string, string> Properties);