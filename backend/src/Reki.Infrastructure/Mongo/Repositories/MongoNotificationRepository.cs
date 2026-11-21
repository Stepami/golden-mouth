using System.Runtime.CompilerServices;
using MongoDB.Bson;
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
    private static readonly FilterDefinition<Notification> ScheduleKey = Builders<Notification>.Filter.Type("_id", BsonType.Binary);

    private readonly IMongoCollection<Notification> _notifications =
        database.GetCollection<Notification>(MongoCollections.Notifications);

    private readonly IMongoCollection<BsonDocument> _documents =
        database.GetCollection<BsonDocument>(MongoCollections.Notifications);

    public async ValueTask<Notification?> GetPendingAsync(PlayerId playerId, CancellationToken cancellationToken)
    {
        var pending = ScheduleKey & Builders<Notification>.Filter.Where(item =>
            item.PlayerId == playerId && item.Status == NotificationStatus.Pending);
        return await _notifications.Find(sessionProvider.Session, pending).FirstOrDefaultAsync(cancellationToken);
    }

    public async IAsyncEnumerable<Notification> GetDueAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        using var cursor = await _notifications.FindAsync(
            ScheduleKey & Builders<Notification>.Filter.Where(item =>
                item.Status == NotificationStatus.Pending && item.SendAt <= now),
            cancellationToken: cancellationToken);
        await foreach (var item in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<Notification> GetDueDeliveriesAsync(
        NotificationChannelType channelType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var filters = Builders<Notification>.Filter;
        var due = filters.Eq("_id.ChannelType", (int)channelType)
            & filters.Where(item => item.Status == NotificationStatus.Pending && item.SendAt <= now);
        using var cursor = await _notifications.Aggregate()
            .Match(due)
            .AppendStage<BsonDocument>(new BsonDocument("$set", new BsonDocument("_id", "$_id.Id")))
            .As<Notification>()
            .ToCursorAsync(cancellationToken);
        await foreach (var item in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public async ValueTask<bool> PrepareDeliveriesAsync(
        Notification notification,
        NotificationChannelType[] channels,
        CancellationToken cancellationToken)
    {
        var session = sessionProvider.Session;
        var result = await _notifications.DeleteOneAsync(
            session,
            item => item.Id == notification.Id && item.Status == NotificationStatus.Pending && item.Version == notification.Version,
            cancellationToken: cancellationToken);
        if (result.DeletedCount == 0)
        {
            return false;
        }

        if (channels.Length > 0)
        {
            var deliveries = channels.Select(channelType =>
            {
                var delivery = notification.ToBsonDocument();
                delivery["_id"] = DeliveryKey(notification.Id, channelType);
                return delivery;
            });
            await _documents.InsertManyAsync(session, deliveries, cancellationToken: cancellationToken);
        }

        return true;
    }

    public async ValueTask SetStatusAsync(
        NotificationId id,
        NotificationChannelType channelType,
        NotificationStatus status,
        CancellationToken cancellationToken) =>
        await _notifications.UpdateOneAsync(
            sessionProvider.Session,
            new BsonDocument("_id", DeliveryKey(id, channelType)),
            Builders<Notification>.Update.Set(nameof(Notification.Status), status).Inc(item => item.Version, 1),
            cancellationToken: cancellationToken);

    public async ValueTask DeleteDeliveryAsync(
        NotificationId id,
        NotificationChannelType channelType,
        CancellationToken cancellationToken) =>
        await _notifications.DeleteOneAsync(new BsonDocument("_id", DeliveryKey(id, channelType)), cancellationToken);

    public async ValueTask<IReadOnlyList<NotificationStatus>> GetLastThreeAsync(
        PlayerId playerId,
        NotificationChannelType channelType,
        CancellationToken cancellationToken)
    {
        var filters = Builders<Notification>.Filter;
        var filter = filters.Eq("_id.ChannelType", (int)channelType)
            & filters.Eq(item => item.PlayerId, playerId)
            & filters.In(item => item.Status, [NotificationStatus.Sent, NotificationStatus.Failed]);
        var recent = await _notifications.Find(sessionProvider.Session, filter)
            .Sort(Builders<Notification>.Sort.Descending(item => item.SendAt).Descending("_id.Id"))
            .Limit(3)
            .Project(item => new { item.Status })
            .ToListAsync(cancellationToken);
        return recent.Select(item => item.Status).ToArray();
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

    private static BsonDocument DeliveryKey(NotificationId id, NotificationChannelType channelType) => new()
    {
        { "Id", new BsonBinaryData(id.Value, GuidRepresentation.Standard) },
        { "ChannelType", (int)channelType },
    };
}
