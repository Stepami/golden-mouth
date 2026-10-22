using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Common.Balance;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class StoreRemainingEnergy(IOptions<GameBalance> balance) : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var baseEnergy = new BsonDocument("$cond", new BsonArray
        {
            "$Settings.Plus",
            balance.Value.PlusPostsPerDay,
            balance.Value.FreePostsPerDay,
        });
        var challengeBonus = new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$gt", new BsonArray { "$ChallengeBonusUntilUtc", "$PostsPeriodStartedAtUtc" }),
            IfNull("$ChallengeDailyBonus", 0),
            0,
        });
        var spent = IfNull("$EnergyUsed", 0);
        var available = new BsonDocument("$subtract", new BsonArray
        {
            new BsonDocument("$add", new BsonArray { baseEnergy, IfNull("$DailyBonus", 0), challengeBonus }),
            spent,
        });
        var initialize = new BsonDocument("$set", new BsonDocument
        {
            { "Energy", IfNull("$Energy", new BsonDocument("$max", new BsonArray { 0, available })) },
            { "EnergyUsed", spent },
        });
        var removeLegacyBonuses = new BsonDocument("$unset", new BsonArray { "DailyBonus", "ChallengeDailyBonus" });
        await db.GetCollection<BsonDocument>(MongoCollections.Players).UpdateManyAsync(
            session,
            FilterDefinition<BsonDocument>.Empty,
            Builders<BsonDocument>.Update.Pipeline(new[] { initialize, removeLegacyBonuses }),
            cancellationToken: cancellationToken);
    }

    private static BsonDocument IfNull(string field, BsonValue fallback) =>
        new("$ifNull", new BsonArray { field, fallback });
}
