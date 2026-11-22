using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Application.Notifications;

public interface INotificationRepository
{
    ValueTask<Notification?> GetPendingAsync(PlayerId playerId, NotificationChannelType channelType, CancellationToken cancellationToken);

    IAsyncEnumerable<Notification> GetDueAsync(NotificationChannelType channelType, CancellationToken cancellationToken);

    ValueTask SaveAsync(Notification notification, CancellationToken cancellationToken);

    ValueTask DeleteForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);
}
