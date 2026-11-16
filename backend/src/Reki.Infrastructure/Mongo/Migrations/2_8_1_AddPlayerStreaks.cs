using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddPlayerStreaks : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 1);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        await players.UpdateManyAsync(
            session,
            Builders<BsonDocument>.Filter.Exists("Streaks", false),
            Builders<BsonDocument>.Update.Set("Streaks", new BsonArray()).Unset("StreakDays"),
            cancellationToken: cancellationToken);
    }
}
