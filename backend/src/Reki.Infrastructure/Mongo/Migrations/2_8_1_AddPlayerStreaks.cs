using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Player.Streaks;
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
            Builders<BsonDocument>.Filter.Empty,
            Builders<BsonDocument>.Update.Unset("Streaks").Unset("StreakDays"),
            cancellationToken: cancellationToken);

        var streaks = db.GetCollection<PlayerStreak>(MongoCollections.PlayerStreaks);
        var keys = Builders<PlayerStreak>.IndexKeys;
        await streaks.Indexes.CreateOneAsync(
            new CreateIndexModel<PlayerStreak>(
                keys.Ascending("PlayerId"),
                new CreateIndexOptions<PlayerStreak>
                {
                    Name = "ux_player_streaks_active",
                    Unique = true,
                    PartialFilterExpression = Builders<PlayerStreak>.Filter.Eq("IsActive", true),
                }),
            cancellationToken: cancellationToken);
    }
}
