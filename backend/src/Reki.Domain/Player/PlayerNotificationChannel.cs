namespace Reki.Domain.Player;

public sealed record PlayerNotificationChannel(
    NotificationChannelType Type,
    IReadOnlyDictionary<string, string> Properties)
{
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