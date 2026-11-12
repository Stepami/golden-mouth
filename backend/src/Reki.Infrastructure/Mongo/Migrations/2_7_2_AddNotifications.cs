using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddNotifications(TimeProvider timeProvider) : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 7, 2);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var players = db.GetCollection<PlayerProfile>(MongoCollections.Players);
        var notifications = db.GetCollection<Notification>(MongoCollections.Notifications);
        var index = new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(item => item.Status).Ascending(item => item.SendAt));
        await notifications.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
        var expiryIndex = new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(item => item.SendAt),
            new CreateIndexOptions<Notification>
            {
                ExpireAfter = TimeSpan.FromDays(30),
                PartialFilterExpression = Builders<Notification>.Filter.Eq(item => item.Status, NotificationStatus.Sent),
            });
        await notifications.Indexes.CreateOneAsync(expiryIndex, cancellationToken: cancellationToken);
        using var cursor = await players.FindAsync(session, FilterDefinition<PlayerProfile>.Empty, cancellationToken: cancellationToken);
        await foreach (var player in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var updated = player.AddExternalUserIds(player.ExternalUserIds);
            await players.UpdateOneAsync(
                session,
                item => item.Id == player.Id,
                Builders<PlayerProfile>.Update.Set(item => item.NotificationChannels, updated.NotificationChannels),
                cancellationToken: cancellationToken);
            if (updated.NotificationChannels.Count > 0)
            {
                var notification = Notification.New(player.Id, startedAt.AddHours(1 + (Random.Shared.NextDouble() * 23)));
                await notifications.InsertOneAsync(session, notification, cancellationToken: cancellationToken);
            }
        }
    }
}
