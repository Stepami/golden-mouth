namespace Reki.Domain.Player;

public sealed record PlayerNotificationChannel(
    NotificationChannelType Type,
    IReadOnlyDictionary<string, string> Properties)
{
    public static PlayerNotificationChannel? FromExternalIds(IEnumerable<ExternalUserId> externalIds)
    {
        var telegram = externalIds.Select(id => id.Value)
            .FirstOrDefault(id => id.StartsWith("telegram:", StringComparison.Ordinal));
        return telegram is null
            ? null
            : new PlayerNotificationChannel(
                NotificationChannelType.Telegram,
                new Dictionary<string, string> { ["chatId"] = telegram[9..] });
    }
}