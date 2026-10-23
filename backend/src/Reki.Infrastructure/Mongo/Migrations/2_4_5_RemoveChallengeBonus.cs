using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class RemoveChallengeBonus : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 5);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        // Remove the retired field before PlayerProfile documents are deserialized.
        // Existing energy is retained; later daily resets no longer grant challenge bonuses.
        await db.GetCollection<BsonDocument>(MongoCollections.Players).UpdateManyAsync(
            session,
            new BsonDocument("ChallengeBonusUntilUtc", new BsonDocument("$exists", true)),
            new BsonDocument("$unset", new BsonDocument("ChallengeBonusUntilUtc", string.Empty)),
            cancellationToken: cancellationToken);
    }
}
