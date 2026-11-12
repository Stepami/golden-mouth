using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Application.Notifications;

public interface INotificationRepository
{
    ValueTask<Notification?> GetPendingAsync(PlayerId playerId, CancellationToken cancellationToken);

    IAsyncEnumerable<Notification> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken);

    ValueTask InsertAsync(Notification notification, CancellationToken cancellationToken);

    ValueTask ReplaceAsync(Notification notification, CancellationToken cancellationToken);

    ValueTask DeleteForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);
}