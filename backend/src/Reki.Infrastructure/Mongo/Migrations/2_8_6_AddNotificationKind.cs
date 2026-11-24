using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddNotificationKind(
    INotificationContentFactory<PlayerProfile> contentFactory) : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 6);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var notifications = db.GetCollection<Notification>(MongoCollections.Notifications);
        var filters = Builders<Notification>.Filter;
        var updates = Builders<Notification>.Update;
        await notifications.UpdateManyAsync(
            session,
            filters.Exists(nameof(Notification.Kind), false),
            updates.Set(nameof(Notification.Kind), NotificationKind.Regular),
            cancellationToken: cancellationToken);

        var pending = filters.Eq(item => item.Status, NotificationStatus.Pending) &
                      filters.Eq(nameof(Notification.Kind), NotificationKind.Regular);
        var players = db.GetCollection<PlayerProfile>(MongoCollections.Players);
        using var playerIds = await notifications.DistinctAsync(session, item => item.PlayerId, pending, cancellationToken: cancellationToken);
        await foreach (var playerId in playerIds.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var player = await players.Find(session, item => item.Id == playerId).FirstOrDefaultAsync(cancellationToken);
            if (player is null)
            {
                continue;
            }

            var content = await contentFactory.GetContent(player);
            await notifications.UpdateManyAsync(
                session,
                pending & filters.Eq(item => item.PlayerId, playerId),
                updates.Set(nameof(Notification.Content), content),
                cancellationToken: cancellationToken);
        }
    }
}
