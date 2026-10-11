using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class MoveReferralStateToInvites : RekiMigrationBase
{
    private static readonly string[] LegacyFields =
    [
        "InvitedBy", "InvitedAtUtc", "FirstPostInviteBonus", "FirstPostInviteBonusUsed", "InviteBonusesEarnedInPeriod"
    ];

    public override DatabaseVersion Version => new(2, 1, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var invites = db.GetCollection<BsonDocument>(MongoCollections.Invites);
        await CreateIndexesAsync(invites, cancellationToken);
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        using var cursor =
            await players.FindAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            await MigrateBatchAsync(players, invites, cursor.Current, cancellationToken);
        }
    }

    public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Referral state cannot be safely merged back into player counters.");

    private static async Task MigrateBatchAsync(IMongoCollection<BsonDocument> players, IMongoCollection<BsonDocument> invites, IEnumerable<BsonDocument> batch, CancellationToken cancellationToken)
    {
        foreach (var player in batch)
        {
            await MigratePlayerAsync(players, invites, player, cancellationToken);
        }
    }

    private static async Task CreateIndexesAsync(
        IMongoCollection<BsonDocument> invites, CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<BsonDocument>(new BsonDocument("InviteeId", 1), new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_invites_invitee", Unique = true,
                PartialFilterExpression = new BsonDocument("InviteeId", new BsonDocument("$type", "string")),
            }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument("InviterId", 1),
                new CreateIndexOptions { Name = "ix_invites_inviter" }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument("PendingTelegramUserId", 1),
                new CreateIndexOptions<BsonDocument>
                {
                    Name = "ux_invites_pending_telegram", Unique = true,
                    PartialFilterExpression =
                        new BsonDocument("PendingTelegramUserId", new BsonDocument("$type", "long")),
                }),
        };
        await invites.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private static async Task MigratePlayerAsync(
        IMongoCollection<BsonDocument> players, IMongoCollection<BsonDocument> invites, BsonDocument player, CancellationToken cancellationToken)
    {
        if (player.TryGetValue("InvitedBy", out var inviter) && inviter.IsString)
        {
            var createdAt = ReadDate(player.GetValue("InvitedAtUtc", BsonNull.Value));
            var invitation = new BsonDocument
            {
                ["_id"] = player["_id"],
                ["InviterId"] = inviter,
                ["InviteeId"] = player["_id"],
                ["CreatedAtUtc"] = createdAt,
                ["BonusPosts"] = player.GetValue("FirstPostInviteBonus", 0),

                // The old implementation already handled the inviter at onboarding,
                // including invitations rejected by its daily cap. Never award again.
                ["InviterBonusUsed"] = true,
                ["InviteeBonusUsed"] = player.GetValue("FirstPostInviteBonusUsed", false),
            };
            await InsertOnceAsync(invites, new BsonDocument("InviteeId", player["_id"]), invitation, cancellationToken);
        }

        var legacyCount = player.GetValue("InviteBonusesEarnedInPeriod", 0).AsInt32;
        if (legacyCount > 0)
        {
            // The old counter cannot identify which friends received the capped grant.
            // Preserve only this migration-day quota, without inventing attribution.
            // Already awarded posts remain in DailyBonus until the normal daily reset.
            var ledger = new BsonDocument
            {
                ["_id"] = $"legacy-quota:{player["_id"].AsString}",
                ["InviterId"] = player["_id"],
                ["LegacyPeriodAtUtc"] = ReadDate(player.GetValue("PostsPeriodStartedAtUtc", BsonNull.Value)),
                ["LegacyInviterBonuses"] = legacyCount,
            };
            await InsertOnceAsync(invites, new BsonDocument("_id", ledger["_id"]), ledger, cancellationToken);
        }

        var unset = new BsonDocument(LegacyFields.Select(field => new BsonElement(field, string.Empty)));

        // Copy first, remove second: interruption at either write is safe to rerun.
        await players.UpdateOneAsync(new BsonDocument("_id", player["_id"]), new BsonDocument("$unset", unset), cancellationToken: cancellationToken);
    }

    private static Task InsertOnceAsync(IMongoCollection<BsonDocument> collection, BsonDocument filter, BsonDocument document, CancellationToken cancellationToken) =>
        collection.UpdateOneAsync(filter, new BsonDocument("$setOnInsert", document), new UpdateOptions { IsUpsert = true }, cancellationToken);

    private static BsonValue ReadDate(BsonValue value)
    {
        if (value.IsBsonNull)
        {
            return BsonNull.Value;
        }

        using var reader = new BsonDocumentReader(new BsonDocument("date", value));
        reader.ReadStartDocument();
        reader.ReadName();
        var date = BsonSerializer.LookupSerializer<DateTimeOffset>().Deserialize(BsonDeserializationContext.CreateRoot(reader));
        return new BsonDateTime(date.UtcDateTime);
    }
}