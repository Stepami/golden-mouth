using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class MoveReferralStateToInvites : RekiMigrationBase
{
    /// <summary>
    /// Freeze the already-issued bonus amount; later balance edits must not change historical credits.
    /// </summary>
    private const int LegacyBonus = 5;
    private static readonly string[] LegacyFields =
        ["InvitedBy", "InvitedAtUtc", "FirstPostInviteBonus", "FirstPostInviteBonusUsed", "InviteBonusesEarnedInPeriod"];

    public override DatabaseVersion Version => new(2, 1, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var invites = db.GetCollection<BsonDocument>(MongoCollections.Invites);
        var inviteeIndex = new CreateIndexModel<BsonDocument>(
            new BsonDocument("InviteeId", 1),
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_invites_invitee",
                Unique = true,
                PartialFilterExpression = new BsonDocument("InviteeId", new BsonDocument("$type", "string")),
            });
        var inviterIndex = new CreateIndexModel<BsonDocument>(
            new BsonDocument("InviterId", 1),
            new CreateIndexOptions { Name = "ix_invites_inviter" });
        await invites.Indexes.CreateManyAsync([inviteeIndex, inviterIndex], cancellationToken);
        await MoveLegacyStateAsync(db, session, cancellationToken);
    }

    private static async Task MoveLegacyStateAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        var invites = db.GetCollection<BsonDocument>(MongoCollections.Invites);
        var legacy = await players.Find(session, Builders<BsonDocument>.Filter.Or(
                LegacyFields.Select(fieldName => Builders<BsonDocument>.Filter.Exists(fieldName))))
            .ToListAsync(cancellationToken);
        foreach (var player in legacy)
        {
            await CopyInvitationAsync(db, session, player, cancellationToken);
        }

        // Copy all attribution before removing any source fields, so interrupted runs remain resumable.
        foreach (var player in legacy)
        {
            var period = ReadTime(player.GetValue("PostsPeriodStartedAtUtc", BsonNull.Value));
            var count = player.GetValue("InviteBonusesEarnedInPeriod", 0).ToInt32();
            var dailyBonus = player.GetValue("DailyBonus", 0).ToInt32();
            var deduction = count < 0 ? dailyBonus : count * LegacyBonus;
            if (count > 0 && period is { } start)
            {
                var invitations = await invites.Find(session, new BsonDocument("InviterId", player["_id"])).ToListAsync(cancellationToken);
                var candidates = invitations.Where(item => ReadTime(item["CreatedAtUtc"]) >= start && ReadTime(item["CreatedAtUtc"]) < start.AddDays(1))
                    .OrderBy(item => ReadTime(item["CreatedAtUtc"])).ThenBy(item => item["_id"])
                    .Take(count).ToList();
                if (candidates.Count == count && deduction <= dailyBonus)
                {
                    await PreserveInviterCreditsAsync(invites, session, candidates, cancellationToken);
                }
            }

            var own = await invites
                .Find(session, new BsonDocument("InviteeId", player["_id"]))
                .FirstOrDefaultAsync(cancellationToken);
            var ownUsedAt = own is null ? null : ReadTime(own.GetValue("InviteeBonusUsedAtUtc", BsonNull.Value));
            if (period is { } ownStart && ownUsedAt >= ownStart &&
                ownUsedAt < ownStart.AddDays(1))
            {
                deduction += player.GetValue("FirstPostInviteBonus", 0).ToInt32();
            }

            var updates = LegacyFields.Select(fieldName => Builders<BsonDocument>.Update.Unset(fieldName)).ToList();
            updates.Add(Builders<BsonDocument>.Update.Set("DailyBonus", Math.Max(0, player.GetValue("DailyBonus", 0).ToInt32() - deduction)));
            await players.UpdateOneAsync(
                session,
                new BsonDocument("_id", player["_id"]),
                Builders<BsonDocument>.Update.Combine(updates),
                cancellationToken: cancellationToken);
        }
    }

    private static async Task CopyInvitationAsync(IMongoDatabase db, IClientSessionHandle session, BsonDocument player, CancellationToken cancellationToken)
    {
        var inviterId = player.GetValue("InvitedBy", BsonNull.Value);
        if (!inviterId.IsString)
        {
            return;
        }

        var created = ReadTime(player.GetValue("InvitedAtUtc", BsonNull.Value)) ??
            ReadTime(player.GetValue("PostsPeriodStartedAtUtc", BsonNull.Value)) ?? DateTimeOffset.UnixEpoch;

        var inviteeUsed = player.GetValue("FirstPostInviteBonusUsed", false).ToBoolean();
        DateTimeOffset? usedAt = null;
        if (inviteeUsed)
        {
            var firstPost = await db.GetCollection<BsonDocument>(MongoCollections.Posts)
                .Find(session, new BsonDocument("PlayerId", player["_id"]))
                .Sort(new BsonDocument("CreatedAtUtc", 1)).FirstOrDefaultAsync(cancellationToken);
            usedAt = firstPost is null ? null : ReadTime(firstPost.GetValue("CreatedAtUtc", BsonNull.Value));
        }

        // Preserve the historical BSON schema independently of today's domain ID types.
        var document = new BsonDocument
        {
            ["_id"] = "legacy_" + player["_id"].AsString,
            ["InviterId"] = inviterId,
            ["InviteeId"] = player["_id"],
            ["CreatedAtUtc"] = WriteTime(created),
            ["InviterBonusUsed"] = true,
            ["InviteeBonusUsed"] = inviteeUsed,
            ["BonusAmount"] = 0,
            ["InviterBonusUsedAtUtc"] = BsonNull.Value,
            ["InviteeBonusUsedAtUtc"] = usedAt is { } used ? WriteTime(used) : BsonNull.Value,
        };
        await db.GetCollection<BsonDocument>(MongoCollections.Invites).UpdateOneAsync(
            session,
            new BsonDocument("InviteeId", player["_id"]),
            new BsonDocument("$setOnInsert", document),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    private static async Task PreserveInviterCreditsAsync(
        IMongoCollection<BsonDocument> invites,
        IClientSessionHandle session,
        IEnumerable<BsonDocument> candidates,
        CancellationToken cancellationToken)
    {
        foreach (var invite in candidates)
        {
            await invites.UpdateOneAsync(
                session,
                new BsonDocument("_id", invite["_id"]),
                new BsonDocument("$set", new BsonDocument
                {
                    ["InviterBonusUsed"] = true,
                    ["InviterBonusUsedAtUtc"] = invite["CreatedAtUtc"],
                }),
                cancellationToken: cancellationToken);
        }
    }

    private static BsonValue WriteTime(DateTimeOffset value)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        writer.WriteStartDocument();
        writer.WriteName("value");
        BsonSerializer.LookupSerializer<DateTimeOffset>().Serialize(BsonSerializationContext.CreateRoot(writer), value);
        writer.WriteEndDocument();
        return document["value"];
    }

    private static DateTimeOffset? ReadTime(BsonValue value)
    {
        if (value.IsBsonNull)
        {
            return null;
        }

        using var reader = new BsonDocumentReader(new BsonDocument("value", value));
        reader.ReadStartDocument();
        _ = reader.ReadName();
        return BsonSerializer.LookupSerializer<DateTimeOffset>()
            .Deserialize(BsonDeserializationContext.CreateRoot(reader)).ToUniversalTime();
    }
}