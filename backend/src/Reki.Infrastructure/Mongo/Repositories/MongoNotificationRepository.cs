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

    public async ValueTask<Notification?> GetPendingAsync(
        PlayerId playerId,
        NotificationChannelType channelType,
        CancellationToken cancellationToken) =>
        await _notifications.Find(
                sessionProvider.Session,
                item => item.PlayerId == playerId && item.ChannelType == channelType && item.Status == NotificationStatus.Pending)
            .FirstOrDefaultAsync(cancellationToken);

    public async IAsyncEnumerable<Notification> GetDueAsync(
        NotificationChannelType channelType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        using var cursor = await _notifications.FindAsync(
            item => item.ChannelType == channelType && item.Status == NotificationStatus.Pending && item.SendAt <= now,
            cancellationToken: cancellationToken);
        await foreach (var item in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public async ValueTask DeleteAsync(NotificationId id, CancellationToken cancellationToken) =>
        await _notifications.DeleteOneAsync(item => item.Id == id, cancellationToken);

    public async ValueTask<IReadOnlyList<Notification>> GetLastThreeAsync(
        PlayerId playerId,
        NotificationChannelType channelType,
        CancellationToken cancellationToken) =>
        await _notifications.Find(
                sessionProvider.Session,
                item => item.PlayerId == playerId && item.ChannelType == channelType &&
                        item.Status != NotificationStatus.Pending)
            .SortByDescending(item => item.SendAt)
            .ThenByDescending(item => item.Id)
            .Limit(3)
            .ToListAsync(cancellationToken);

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
