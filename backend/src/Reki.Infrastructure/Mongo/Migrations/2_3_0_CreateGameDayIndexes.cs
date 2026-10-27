using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
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
        await db.GetCollection<PlayerProfile>(MongoCollections.Players).Indexes.CreateOneAsync(
            new CreateIndexModel<PlayerProfile>(
                Builders<PlayerProfile>.IndexKeys.Ascending(player => player.PostsPeriodStartedAtUtc),
                new CreateIndexOptions { Name = "ix_players_day_period" }),
            cancellationToken: cancellationToken);
        await db.GetCollection<World>(MongoCollections.Worlds).Indexes.CreateOneAsync(
            new CreateIndexModel<World>(
                Builders<World>.IndexKeys.Ascending(world => world.Player.PeriodStartedAtUtc),
                new CreateIndexOptions { Name = "ix_worlds_day_period" }),
            cancellationToken: cancellationToken);
    }
}
