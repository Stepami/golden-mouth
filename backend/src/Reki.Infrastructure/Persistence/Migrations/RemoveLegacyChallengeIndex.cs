using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Social;

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

        // Only the original unique PlayerId index is owned by this migration.
        // A reused name with different semantics requires operator investigation.
        if (legacy["key"] != new BsonDocument("PlayerId", 1) ||
            legacy.GetValue("unique", false) != BsonBoolean.True ||
            legacy.GetValue("sparse", false) != BsonBoolean.False ||
            legacy.Contains("partialFilterExpression") ||
            legacy.Contains("collation") ||
            legacy.Contains("expireAfterSeconds"))
        {
            throw new InvalidOperationException(
                "The legacy challenge index has an unexpected definition; no index was removed.");
        }

        // InitialGameIndexes must have installed the replacement before any destructive DDL.
        if (!indexes.Any(index => index["name"] == "ux_challenges_active_participants" &&
                                  index["key"] == new BsonDocument("Participants", 1) &&
                                  index.GetValue("unique", false) == BsonBoolean.True &&
                                  index.GetValue("partialFilterExpression", BsonNull.Value) ==
                                  new BsonDocument("Status", (int)ChallengeStatus.Active)))
        {
            throw new InvalidOperationException(
                "The replacement challenge index is missing or incompatible; no index was removed.");
        }

        await challenges.Indexes.DropOneAsync(LegacyIndexName, cancellationToken);
        using var remainingCursor = await challenges.Indexes.ListAsync(cancellationToken);
        var remaining = await remainingCursor.ToListAsync(cancellationToken);
        if (remaining.Any(index => index["name"] == LegacyIndexName))
        {
            throw new InvalidOperationException("The legacy challenge index still exists after removal.");
        }
    }

    public override Task DownAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "The obsolete unique challenge index cannot be safely restored over current player data.");
    }
}