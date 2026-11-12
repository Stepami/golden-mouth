using Reki.Domain.Player;

namespace Reki.Domain.Notifications;

public sealed record Notification(
    NotificationId Id,
    PlayerId PlayerId,
    NotificationStatus Status,
    DateTimeOffset SendAt,
    string Content)
{
    public const string DefaultContent = "Пора попадать в РЕКИ! Заходи в игру и пиши посты - тебя ждёт успех!";

    public static Notification New(PlayerId playerId, DateTimeOffset sendAt, string content = DefaultContent) =>
        new(NotificationId.From(sendAt), playerId, NotificationStatus.Pending, sendAt, content);
}