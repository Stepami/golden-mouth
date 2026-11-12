using System.Runtime.CompilerServices;
using MongoDB.Driver;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoNotificationRepository(IMongoDatabase database, MongoSessionProvider sessionProvider)
    : INotificationRepository
{
    private readonly IMongoCollection<Notification> _notifications =
        database.GetCollection<Notification>(MongoCollections.Notifications);

    public async ValueTask<Notification?> GetPendingAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await (sessionProvider.Current is { } session
                ? _notifications.Find(
                    session,
                    item => item.PlayerId == playerId && item.Status == NotificationStatus.Pending)
                : _notifications.Find(item => item.PlayerId == playerId && item.Status == NotificationStatus.Pending))
            .FirstOrDefaultAsync(cancellationToken);

    public async IAsyncEnumerable<Notification> GetDueAsync(
        DateTimeOffset now,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var cursor = await _notifications.FindAsync(
            item => item.Status == NotificationStatus.Pending && item.SendAt <= now,
            cancellationToken: cancellationToken);
        await foreach (var item in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public async ValueTask InsertAsync(Notification notification, CancellationToken cancellationToken)
    {
        if (sessionProvider.Current is { } session)
        {
            await _notifications.InsertOneAsync(session, notification, cancellationToken: cancellationToken);
        }
        else
        {
            await _notifications.InsertOneAsync(notification, cancellationToken: cancellationToken);
        }
    }

    public async ValueTask ReplaceAsync(Notification notification, CancellationToken cancellationToken) =>
        await _notifications.ReplaceOneAsync(
            sessionProvider,
            item => item.Id == notification.Id,
            notification,
            cancellationToken);

    public async ValueTask DeleteAsync(NotificationId id, CancellationToken cancellationToken) =>
        await _notifications.DeleteOneAsync(item => item.Id == id, cancellationToken);

    public async ValueTask DeleteForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _notifications.DeleteManyAsync(
            sessionProvider.Session,
            item => item.PlayerId == playerId,
            cancellationToken: cancellationToken);
}