using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Notifications;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddNotificationKind : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 6);

    public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
        db.GetCollection<BsonDocument>(MongoCollections.Notifications).UpdateManyAsync(
            session,
            FilterDefinition<BsonDocument>.Empty,
            Builders<BsonDocument>.Update.Set(nameof(Notification.Kind), (int)NotificationKind.Regular),
            cancellationToken: cancellationToken);
}
