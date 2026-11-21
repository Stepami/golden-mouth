using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Domain.Notifications;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class SplitNotificationDeliveries : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 4);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var notifications = db.GetCollection<Notification>(MongoCollections.Notifications);
        using var cursor = await notifications.Indexes.ListAsync(cancellationToken);
        var indexes = await cursor.ToListAsync(cancellationToken);
        if (indexes.Any(index => index["name"] == "SendAt_1"))
        {
            await notifications.Indexes.DropOneAsync("SendAt_1", cancellationToken);
        }

        var keys = Builders<Notification>.IndexKeys;
        var completed = Builders<Notification>.Filter.In(item => item.Status, [NotificationStatus.Sent, NotificationStatus.Failed]);
        var history = new CreateIndexModel<Notification>(
            keys.Ascending(item => item.PlayerId).Ascending("_id.ChannelType").Descending(item => item.SendAt).Descending("_id.Id"),
            new CreateIndexOptions<Notification> { PartialFilterExpression = completed });
        var expiry = new CreateIndexModel<Notification>(
            keys.Ascending(item => item.SendAt),
            new CreateIndexOptions<Notification>
            {
                Name = "ttl_notifications_completed",
                ExpireAfter = TimeSpan.FromDays(30),
                PartialFilterExpression = completed,
            });
        await notifications.Indexes.CreateManyAsync([history, expiry], cancellationToken);
    }
}
