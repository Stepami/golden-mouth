using Reki.Domain.Player;

namespace Reki.Application.Notifications;

public static class NotificationSubscriptions
{
    public static IReadOnlyList<PlayerNotificationChannel> Create(IEnumerable<ExternalUserId> externalIds)
    {
        var telegram = externalIds.Select(id => id.Value)
            .FirstOrDefault(id => id.StartsWith("telegram:", StringComparison.Ordinal));
        PlayerNotificationChannel[] channels = telegram is null
            ? []
            : [new(NotificationChannelType.Telegram, new Dictionary<string, string> { ["chatId"] = telegram[9..] })];
        return channels;
    }

    public static IReadOnlyList<PlayerNotificationChannel> WithChannel(
        IReadOnlyList<PlayerNotificationChannel> channels,
        PlayerNotificationChannel channel) =>
        [.. channels.Where(item => item.Type != channel.Type), channel];
}