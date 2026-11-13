using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class RemoveGameDay : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 7, 3);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        using var collectionCursor = await db.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        var collectionNames = await collectionCursor.ToListAsync(cancellationToken);
        foreach (var (collectionName, indexName) in new[]
                 {
                     (MongoCollections.Players, "ix_players_day_period"),
                     (MongoCollections.Worlds, "ix_worlds_day_period"),
                 })
        {
            if (!collectionNames.Contains(collectionName))
            {
                continue;
            }

            var collection = db.GetCollection<BsonDocument>(collectionName);
            using var indexes = await collection.Indexes.ListAsync(cancellationToken);
            if ((await indexes.ToListAsync(cancellationToken)).Any(index => index["name"] == indexName))
            {
                await collection.Indexes.DropOneAsync(indexName, cancellationToken);
            }
        }

        if (collectionNames.Contains("events"))
        {
            await db.DropCollectionAsync("events", cancellationToken);
        }

        await db.GetCollection<BsonDocument>(MongoCollections.Players).UpdateManyAsync(
            session,
            FilterDefinition<BsonDocument>.Empty,
            new BsonDocument("$unset", new BsonDocument { ["PostsPeriodStartedAtUtc"] = 1, ["Wallet.EnergyUsed"] = 1 }),
            cancellationToken: cancellationToken);

        foreach (var name in new[] { MongoCollections.Worlds, MongoCollections.WorldTemplates })
        {
            var collection = db.GetCollection<BsonDocument>(name);
            await collection.UpdateManyAsync(
                session,
                FilterDefinition<BsonDocument>.Empty,
                new BsonDocument("$unset", new BsonDocument
                {
                    ["Player.Wants"] = 1,
                    ["Player.LastPostAtUtc"] = 1,
                    ["Player.RestedPostsRemaining"] = 1,
                    ["Player.PeriodStartedAtUtc"] = 1,
                }),
                cancellationToken: cancellationToken);
            await collection.UpdateManyAsync(
                session,
                new BsonDocument("Stars", new BsonDocument("$type", "array")),
                new BsonDocument("$unset", new BsonDocument
                {
                    ["Stars.$[].Relation"] = 1,
                    ["Stars.$[].RelationGainedInPeriod"] = 1,
                    ["Stars.$[].RelationPeriodStartedAtUtc"] = 1,
                }),
                cancellationToken: cancellationToken);
            await collection.UpdateManyAsync(
                session,
                new BsonDocument("Player.Achievements", new BsonDocument("$type", "array")),
                new BsonDocument("$pull", new BsonDocument("Player.Achievements", new BsonDocument("Id", "star_card"))),
                cancellationToken: cancellationToken);
        }

        foreach (var name in new[] { "ticker_CronTickers", "ticker_TimeTickers" }.Where(collectionNames.Contains))
        {
            await db.GetCollection<BsonDocument>(name).DeleteManyAsync(
                session,
                new BsonDocument("Function", "ResetGameDayJob"),
                cancellationToken: cancellationToken);
        }
    }
}
