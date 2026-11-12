using Reki.Domain.Player;

namespace Reki.Domain.Notifications;

public sealed record Notification(
    NotificationId Id,
    PlayerId PlayerId,
    NotificationStatus Status,
    DateTimeOffset SendAt,
    string Content,
    DateTime? ExpiresAtUtc = null)
{
    public const string DefaultContent = "Загляните в Реки — вас ждут новые истории.";

    public static Notification New(PlayerId playerId, DateTimeOffset sendAt, string content = DefaultContent) =>
        new(NotificationId.From(sendAt), playerId, NotificationStatus.Pending, sendAt, content);
}