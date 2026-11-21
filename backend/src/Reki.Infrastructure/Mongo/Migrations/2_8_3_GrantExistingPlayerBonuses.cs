using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Domain.Bonuses;
using Reki.Domain.Player;
using Reki.Domain.Posts;
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
        await BackfillPublishedPostCount(db, players, session, cancellationToken);

        using var cursor = await players.Find(session, FilterDefinition<PlayerProfile>.Empty)
            .Project(player => player.Id)
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            var playerIds = cursor.Current.ToArray();
            var grantIds = playerIds.Select(playerId => BonusId.From(playerId.Value));
            var existing = await bonuses
                .Find(session, Builders<Bonus>.Filter.In(bonus => bonus.Id, grantIds))
                .Project(bonus => bonus.Id)
                .ToListAsync(cancellationToken);
            var granted = existing.ToHashSet();
            var pending = playerIds.Where(playerId => !granted.Contains(BonusId.From(playerId.Value))).ToArray();
            if (pending.Length == 0)
            {
                continue;
            }

            var update = Builders<PlayerProfile>.Update
                .Inc(player => player.Wallet.Crystals, 2)
                .Inc(player => player.Version, 1);
            await players.UpdateManyAsync(
                session,
                Builders<PlayerProfile>.Filter.In(player => player.Id, pending),
                update,
                cancellationToken: cancellationToken);
            await bonuses.InsertManyAsync(session, pending.SelectMany(GetGrant), cancellationToken: cancellationToken);
        }
    }

    private static async Task BackfillPublishedPostCount(
        IMongoDatabase db,
        IMongoCollection<PlayerProfile> players,
        IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        var counts = await db.GetCollection<GamePost>(MongoCollections.Posts)
            .Aggregate(session)
            .Group(post => post.AuthorId, group => new { Author = group.Key, Count = group.LongCount() })
            .ToListAsync(cancellationToken);
        var countByAuthor = counts.ToDictionary(count => count.Author, count => count.Count);
        var filters = Builders<PlayerProfile>.Filter;
        var missingCount = filters.Exists(player => player.PublishedPostCount, false);
        var updates = Builders<PlayerProfile>.Update;
        using var cursor = await players.Find(session, missingCount).Project(player => player.Id).ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            var batch = cursor.Current.Select(playerId => new UpdateOneModel<PlayerProfile>(
                missingCount & filters.Eq(player => player.Id, playerId),
                updates.Set(player => player.PublishedPostCount, countByAuthor.GetValueOrDefault(playerId.ToAuthorId())))).ToArray();
            if (batch.Length > 0)
            {
                await players.BulkWriteAsync(session, batch, cancellationToken: cancellationToken);
            }
        }
    }

    private static Bonus[] GetGrant(PlayerId playerId) =>
    [
        // The first card doubles as the per-player grant marker and is retained after being claimed.
        new ViralCardBonus(BonusId.From(playerId.Value), playerId, 5),
        new ViralCardBonus(BonusId.New(), playerId, 5),
        new ViralCardBonus(BonusId.New(), playerId, 5),
        new FlopGuardBonus(BonusId.New(), playerId, 10),
        new FlopGuardBonus(BonusId.New(), playerId, 10),
        new FlopGuardBonus(BonusId.New(), playerId, 10),
    ];
}
