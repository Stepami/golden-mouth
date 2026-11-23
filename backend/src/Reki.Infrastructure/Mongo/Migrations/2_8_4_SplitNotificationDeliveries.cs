using System.Globalization;
using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class SplitNotificationDeliveries : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 4);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var notifications = db.GetCollection<Notification>(MongoCollections.Notifications);
        await ReplaceIndexesAsync(notifications, cancellationToken);
        await notifications.DeleteManyAsync(session, item => item.Status == NotificationStatus.Sent, cancellationToken: cancellationToken);

        await MigrateChannelsAsync(db.GetCollection<BsonDocument>(MongoCollections.Players), session, cancellationToken);
        var players = db.GetCollection<PlayerProfile>(MongoCollections.Players);
        var filters = Builders<Notification>.Filter;
        var pending = filters.Eq(item => item.Status, NotificationStatus.Pending) & filters.Exists(item => item.ChannelType, false);
        using var cursor = await notifications.FindAsync(session, pending, cancellationToken: cancellationToken);
        await foreach (var legacy in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var channels = await players.Find(session, item => item.Id == legacy.PlayerId)
                .Project(item => item.NotificationChannels)
                .FirstOrDefaultAsync(cancellationToken);
            var types = channels?.Keys.ToArray() ?? [];
            if (types.Length == 0)
            {
                await notifications.DeleteOneAsync(session, item => item.Id == legacy.Id, cancellationToken: cancellationToken);
                continue;
            }

            var copies = types.Skip(1).Select(channelType => (WriteModel<Notification>)new InsertOneModel<Notification>(
                new Notification(NotificationId.From(legacy.SendAt), legacy.PlayerId, channelType, legacy.Status, legacy.SendAt, legacy.Content, legacy.Version)));
            var original = new UpdateOneModel<Notification>(
                filters.Eq(item => item.Id, legacy.Id),
                Builders<Notification>.Update.Set(item => item.ChannelType, types[0]));
            await notifications.BulkWriteAsync(session, copies.Append(original), cancellationToken: cancellationToken);
        }
    }

    private static async Task MigrateChannelsAsync(
        IMongoCollection<BsonDocument> players, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        const string field = nameof(PlayerProfile.NotificationChannels);
        var filters = Builders<BsonDocument>.Filter;
        using var cursor = await players.Find(session, filters.Type(field, BsonType.Array) | filters.Exists(field, false))
            .Project(Builders<BsonDocument>.Projection.Include(field))
            .ToCursorAsync(cancellationToken);
        await foreach (var player in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var legacy = player.GetValue(field, new BsonArray()).AsBsonArray;
            await players.UpdateOneAsync(
                session,
                filters.Eq("_id", player["_id"]),
                Builders<BsonDocument>.Update.Set(field, ConvertChannels(legacy)),
                cancellationToken: cancellationToken);
        }
    }

    private static BsonDocument ConvertChannels(BsonArray legacy)
    {
        var channels = new BsonDocument();
        foreach (var entries in legacy.GroupBy(item => (NotificationChannelType)item["Type"].AsInt32))
        {
            var properties = entries.Select(item => item["Properties"]);
            PlayerNotificationChannel channel = entries.Key switch
            {
                NotificationChannelType.Telegram => new TelegramNotificationChannel(
                    long.Parse(properties.Last()["chatId"].AsString, CultureInfo.InvariantCulture)),
                NotificationChannelType.WebPush => new WebPushNotificationChannel(properties
                    .GroupBy(item => item["endpoint"].AsString, StringComparer.Ordinal)
                    .Select(group => group.Last())
                    .Select(item => new WebPushSubscription(item["endpoint"].AsString, item["p256dh"].AsString, item["auth"].AsString))
                    .ToDictionary(item => item.Endpoint, StringComparer.Ordinal)),
                _ => throw new InvalidOperationException($"Unknown legacy notification channel type {entries.Key}."),
            };
            channels.Add(entries.Key.ToString(), channel.ToBsonDocument());
        }

        return channels;
    }

    private static async Task ReplaceIndexesAsync(IMongoCollection<Notification> notifications, CancellationToken cancellationToken)
    {
        using var cursor = await notifications.Indexes.ListAsync(cancellationToken);
        var indexes = await cursor.ToListAsync(cancellationToken);
        foreach (var index in indexes)
        {
            var name = index["name"].AsString;
            if (name is "SendAt_1" or "Status_1_SendAt_1")
            {
                await notifications.Indexes.DropOneAsync(name, cancellationToken);
            }
        }

        var keys = Builders<Notification>.IndexKeys;
        var completed = Builders<Notification>.Filter.In(item => item.Status, [NotificationStatus.Sent, NotificationStatus.Failed]);
        var due = new CreateIndexModel<Notification>(
            keys.Ascending(item => item.ChannelType).Ascending(item => item.Status).Ascending(item => item.SendAt));
        var history = new CreateIndexModel<Notification>(
            keys.Ascending(item => item.PlayerId).Ascending(item => item.ChannelType).Descending(item => item.SendAt).Descending(item => item.Id));
        var expiry = new CreateIndexModel<Notification>(
            keys.Ascending(item => item.SendAt),
            new CreateIndexOptions<Notification>
            {
                Name = "ttl_notifications_completed",
                ExpireAfter = TimeSpan.FromDays(30),
                PartialFilterExpression = completed,
            });
        await notifications.Indexes.CreateManyAsync([due, history, expiry], cancellationToken);
    }
}
