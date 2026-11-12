using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Application.Notifications;

public interface INotificationRepository
{
    ValueTask UpsertPendingAsync(Notification notification, bool reschedule, CancellationToken cancellationToken);

    IAsyncEnumerable<Notification> GetDueAsync(CancellationToken cancellationToken);

    ValueTask SaveAsync(Notification notification, CancellationToken cancellationToken);

    ValueTask DeleteForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);
}