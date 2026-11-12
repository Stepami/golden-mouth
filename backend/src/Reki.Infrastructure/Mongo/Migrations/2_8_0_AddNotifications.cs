using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddNotifications(TimeProvider timeProvider) : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var players = db.GetCollection<PlayerProfile>(MongoCollections.Players);
        var notifications = db.GetCollection<Notification>(MongoCollections.Notifications);
        var index = new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(item => item.Status).Ascending(item => item.SendAt));
        await notifications.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
        using var cursor = await players.FindAsync(session, FilterDefinition<PlayerProfile>.Empty, cancellationToken: cancellationToken);
        await foreach (var player in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var channels = NotificationSubscriptions.Create(player.ExternalUserIds);
            await players.UpdateOneAsync(
                session,
                item => item.Id == player.Id,
                Builders<PlayerProfile>.Update.Set(item => item.NotificationChannels, channels),
                cancellationToken: cancellationToken);
            if (channels.Count > 0)
            {
                var notification = NotificationSchedule.Create(player.Id, startedAt) with
                {
                    SendAt = startedAt.AddHours(1 + (Random.Shared.NextDouble() * 23)),
                };
                await notifications.InsertOneAsync(session, notification, cancellationToken: cancellationToken);
            }
        }
    }
}
