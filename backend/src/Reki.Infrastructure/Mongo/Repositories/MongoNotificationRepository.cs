using System.Runtime.CompilerServices;
using MongoDB.Driver;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoNotificationRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider,
    TimeProvider timeProvider)
    : INotificationRepository
{
    private readonly IMongoCollection<Notification> _notifications =
        database.GetCollection<Notification>(MongoCollections.Notifications);

    public async ValueTask UpsertPendingAsync(
        Notification notification,
        bool reschedule,
        CancellationToken cancellationToken)
    {
        var updates = Builders<Notification>.Update
            .SetOnInsert(item => item.Id, notification.Id)
            .SetOnInsert(item => item.Content, notification.Content);
        updates = reschedule
            ? updates.Set(item => item.SendAt, notification.SendAt)
            : updates.SetOnInsert(item => item.SendAt, notification.SendAt);
        await _notifications.UpdateOneAsync(
            sessionProvider.Session,
            item => item.PlayerId == notification.PlayerId && item.Status == NotificationStatus.Pending,
            updates,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    public async IAsyncEnumerable<Notification> GetDueAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        using var cursor = await _notifications.FindAsync(
            item => item.Status == NotificationStatus.Pending && item.SendAt <= now,
            cancellationToken: cancellationToken);
        await foreach (var item in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public async ValueTask SaveAsync(Notification notification, CancellationToken cancellationToken) =>
        await _notifications.ReplaceOneAsync(
            sessionProvider,
            item => item.Id == notification.Id,
            notification,
            cancellationToken,
            new ReplaceOptions { IsUpsert = true });

    public async ValueTask DeleteForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _notifications.DeleteManyAsync(
            sessionProvider.Session,
            item => item.PlayerId == playerId,
            cancellationToken: cancellationToken);
}