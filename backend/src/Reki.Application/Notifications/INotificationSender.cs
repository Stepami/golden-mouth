using Reki.Domain.Notifications;

namespace Reki.Application.Notifications;

public interface INotificationSender
{
    Task<NotificationStatus> SendAsync(
        PlayerNotificationChannel channel,
        Notification notification,
        CancellationToken ct);
}
