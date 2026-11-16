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

    public async ValueTask<Notification?> GetPendingAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _notifications.Find(
                sessionProvider.Session,
                item => item.PlayerId == playerId && item.Status == NotificationStatus.Pending)
            .FirstOrDefaultAsync(cancellationToken);

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
