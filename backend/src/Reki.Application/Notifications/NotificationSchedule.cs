using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Application.Notifications;

public static class NotificationSchedule
{
    public const string Content = "Загляните в Реки — вас ждут новые истории.";

    public static Notification Create(PlayerId playerId, DateTimeOffset now) =>
        new(NotificationId.New(), playerId, NotificationStatus.Pending, now.AddDays(1), Content);
}