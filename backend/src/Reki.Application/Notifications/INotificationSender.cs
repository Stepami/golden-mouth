using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Application.Notifications;

public interface INotificationSender
{
    Task<NotificationStatus> SendAsync(
        PlayerNotificationChannel channel,
        Notification notification,
        CancellationToken ct);
}
