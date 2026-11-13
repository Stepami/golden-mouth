using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class CreateGameDayIndexes : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 3, 0);

    public override async Task UpAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        await db.GetCollection<BsonDocument>(MongoCollections.Players).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("PostsPeriodStartedAtUtc"),
                new CreateIndexOptions { Name = "ix_players_day_period" }),
            cancellationToken: cancellationToken);
        await db.GetCollection<BsonDocument>(MongoCollections.Worlds).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("Player.PeriodStartedAtUtc"),
                new CreateIndexOptions { Name = "ix_worlds_day_period" }),
            cancellationToken: cancellationToken);
    }
}
