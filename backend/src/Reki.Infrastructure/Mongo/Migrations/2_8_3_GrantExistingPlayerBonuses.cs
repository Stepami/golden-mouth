using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Domain.Bonuses;
using Reki.Domain.Player;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class GrantExistingPlayerBonuses : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 3);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var bonuses = db.GetCollection<Bonus>(MongoCollections.Bonuses);
        var keys = Builders<Bonus>.IndexKeys;
        await bonuses.Indexes.CreateOneAsync(
            new CreateIndexModel<Bonus>(
                keys.Ascending(bonus => bonus.PlayerId)
                    .Ascending("_t")
                    .Ascending(bonus => bonus.IsClaimed)
                    .Ascending(bonus => bonus.Id),
                new CreateIndexOptions { Name = "ix_bonuses_player_active" }),
            cancellationToken: cancellationToken);

        var players = db.GetCollection<PlayerProfile>(MongoCollections.Players);

        using var cursor = await players.Find(session, FilterDefinition<PlayerProfile>.Empty)
            .Project(player => player.Id)
            .ToCursorAsync(cancellationToken);
        await foreach (var playerId in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            await bonuses.InsertManyAsync(
                session,
                [
                    new ViralCardBonus(BonusId.New(), playerId, 5),
                    new ViralCardBonus(BonusId.New(), playerId, 5),
                    new ViralCardBonus(BonusId.New(), playerId, 5),
                    new FlopGuardBonus(BonusId.New(), playerId, 10),
                    new FlopGuardBonus(BonusId.New(), playerId, 10),
                    new FlopGuardBonus(BonusId.New(), playerId, 10),
                ],
                cancellationToken: cancellationToken);
        }

        await players.UpdateManyAsync(
            session,
            FilterDefinition<PlayerProfile>.Empty,
            Builders<PlayerProfile>.Update.Inc(player => player.Wallet.Crystals, 2).Inc(player => player.Version, 1),
            cancellationToken: cancellationToken);
    }
}
