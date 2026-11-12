using Reki.Domain.Player;

namespace Reki.Domain.Notifications;

public sealed record Notification(
    NotificationId Id,
    PlayerId PlayerId,
    NotificationStatus Status,
    DateTimeOffset SendAt,
    string Content);