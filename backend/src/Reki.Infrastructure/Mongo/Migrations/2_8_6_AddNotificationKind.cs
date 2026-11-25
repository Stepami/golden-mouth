using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Domain.Player.Streaks;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddNotificationKind(
    INotificationContentFactory<PlayerProfile> contentFactory,
    INotificationContentFactory<PlayerStreak> streakContentFactory,
    TimeProvider timeProvider) : RekiMigrationBase
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

        var streaks = db.GetCollection<PlayerStreak>(MongoCollections.PlayerStreaks);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var tomorrow = today.AddDays(1);
        using var active = await streaks.FindAsync(session, item => item.IsActive && item.Count > 0, cancellationToken: cancellationToken);
        await foreach (var streak in active.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var reminderDay = DateOnly.FromDateTime(streak.StartedAt.UtcDateTime).AddDays(streak.Count);
            if (reminderDay != today && reminderDay != tomorrow)
            {
                continue;
            }

            var player = await players.Find(session, item => item.Id == streak.PlayerId).FirstOrDefaultAsync(cancellationToken);
            if (player is null || player.NotificationChannels.Count == 0)
            {
                continue;
            }

            var content = await streakContentFactory.GetContent(streak);
            var sendAt = new DateTimeOffset(reminderDay, new TimeOnly(20, 0), TimeSpan.FromHours(3));
            var reminders = player.NotificationChannels.Keys.Select(channel =>
                Notification.New(player.Id, channel, sendAt, content, NotificationKind.Streak));
            await notifications.InsertManyAsync(session, reminders, cancellationToken: cancellationToken);
        }
    }
}
