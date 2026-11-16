using System.Collections.ObjectModel;

namespace Reki.Domain.Player;

public sealed record PlayerNotificationChannel
{
    public PlayerNotificationChannel(NotificationChannelType type, IReadOnlyDictionary<string, string> properties)
    {
        Type = type;
        Properties = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(properties));
    }

    public NotificationChannelType Type { get; }

    public IReadOnlyDictionary<string, string> Properties { get; }

    public static PlayerNotificationChannel Telegram(long chatId) =>
        new(
            NotificationChannelType.Telegram,
            new Dictionary<string, string>
                { ["chatId"] = chatId.ToString(System.Globalization.CultureInfo.InvariantCulture) });

    public static PlayerNotificationChannel WebPush(string endpoint, string p256dh, string auth) =>
        new(
            NotificationChannelType.WebPush,
            new Dictionary<string, string> { ["endpoint"] = endpoint, ["p256dh"] = p256dh, ["auth"] = auth });
}
