using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class RemoveLegacyChallengeIndex : RekiMigrationBase
{
    private const string LegacyIndexName = "ux_challenges_active_player";

    public override DatabaseVersion Version => new(1, 1, 0);

    public override async Task UpAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        var challenges = db.GetCollection<BsonDocument>(MongoCollections.Challenges);
        using var cursor = await challenges.Indexes.ListAsync(cancellationToken);
        var indexes = await cursor.ToListAsync(cancellationToken);
        var legacy = indexes.SingleOrDefault(index => index["name"] == LegacyIndexName);
        if (legacy is null)
        {
            return;
        }

        await challenges.Indexes.DropOneAsync(LegacyIndexName, cancellationToken);
    }
}
