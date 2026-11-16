using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddEntityVersions : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 7, 9);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        string[] collections =
        [
            MongoCollections.Players,
            MongoCollections.Worlds,
            MongoCollections.WorldTemplates,
            MongoCollections.Posts,
            MongoCollections.Invites,
            MongoCollections.Notifications,
        ];
        foreach (var name in collections)
        {
            var collection = db.GetCollection<BsonDocument>(name);
            var filter = Builders<BsonDocument>.Filter.Eq("Version", BsonNull.Value);
            await collection.UpdateManyAsync(session, filter, Builders<BsonDocument>.Update.Set("Version", 0), cancellationToken: cancellationToken);
        }
    }
}
