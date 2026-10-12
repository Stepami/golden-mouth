using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class MoveReferralStateToInvites : RekiMigrationBase
{
    // Freeze the already-issued bonus amount; later balance edits must not change historical credits.
    private const int LegacyBonus = 5;
    private static readonly string[] LegacyFields =
        ["InvitedBy", "InvitedAtUtc", "FirstPostInviteBonus", "FirstPostInviteBonusUsed", "InviteBonusesEarnedInPeriod"];

    public override DatabaseVersion Version => new(2, 1, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var invites = db.GetCollection<BsonDocument>(MongoCollections.Invites);
        var inviteeIndex = new CreateIndexModel<BsonDocument>(
            new BsonDocument(nameof(Invitation.InviteeId), 1),
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_invites_invitee",
                Unique = true,
                PartialFilterExpression = new BsonDocument("InviteeId", new BsonDocument("$type", "string")),
            });
        var inviterIndex = new CreateIndexModel<BsonDocument>(
            new BsonDocument(nameof(Invitation.InviterId), 1),
            new CreateIndexOptions { Name = "ix_invites_inviter" });
        await invites.Indexes.CreateManyAsync([inviteeIndex, inviterIndex], cancellationToken);
        await MoveLegacyStateAsync(db, session, cancellationToken);
    }

    public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Moved referral state cannot be safely rolled back.");

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

        var deductions = new List<(BsonDocument Player, int Amount)>();

        // Copy all attribution before removing any source fields, so interrupted runs remain resumable.
        foreach (var player in legacy)
        {
            var period = ReadTime(player.GetValue("PostsPeriodStartedAtUtc", BsonNull.Value));
            var count = player.GetValue("InviteBonusesEarnedInPeriod", 0).ToInt32();
            if (count < 0)
            {
                throw new InvalidOperationException("Legacy referral credit count is invalid. Source fields were preserved.");
            }

            var deduction = count * LegacyBonus;
            if (count > 0 && period is null)
            {
                throw new InvalidOperationException("Legacy referral period is missing. Source fields were preserved.");
            }

            if (count > 0 && period is { } start)
            {
                var candidates = await db.GetCollection<Invitation>(MongoCollections.Invites)
                    .Find(session, item => item.InviterId == PlayerId.From(player["_id"].AsString) &&
                        item.CreatedAtUtc >= start && item.CreatedAtUtc < start.AddDays(1))
                    .SortBy(item => item.CreatedAtUtc).ThenBy(item => item.Id)
                    .Limit(count).ToListAsync(cancellationToken);
                if (candidates.Count != count)
                {
                    throw new InvalidOperationException("Legacy referral credits have incomplete attribution. Source fields were preserved.");
                }

                await PreserveInviterCreditsAsync(invites, session, candidates, cancellationToken);
            }

            var own = await db.GetCollection<Invitation>(MongoCollections.Invites)
                .Find(session, item => item.InviteeId == PlayerId.From(player["_id"].AsString))
                .FirstOrDefaultAsync(cancellationToken);
            if (period is { } ownStart && own?.InviteeBonusUsedAtUtc >= ownStart &&
                own.InviteeBonusUsedAtUtc < ownStart.AddDays(1))
            {
                deduction += player.GetValue("FirstPostInviteBonus", 0).ToInt32();
            }

            if (deduction > player.GetValue("DailyBonus", 0).ToInt32())
            {
                throw new InvalidOperationException("Legacy referral credits exceed the stored daily bonus. Source fields were preserved.");
            }

            deductions.Add((player, deduction));
        }

        foreach ((var player, var deduction) in deductions)
        {
            var updates = LegacyFields.Select(fieldName => Builders<BsonDocument>.Update.Unset(fieldName)).ToList();
            updates.Add(Builders<BsonDocument>.Update.Inc("DailyBonus", -deduction));
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

        var created = ReadTime(player.GetValue("InvitedAtUtc", BsonNull.Value));
        if (created is null)
        {
            throw new InvalidOperationException("Legacy invitation creation time is missing. Source fields were preserved.");
        }

        var inviteeUsed = player.GetValue("FirstPostInviteBonusUsed", false).ToBoolean();
        DateTimeOffset? usedAt = null;
        if (inviteeUsed)
        {
            var firstPost = await db.GetCollection<BsonDocument>(MongoCollections.Posts)
                .Find(session, new BsonDocument("PlayerId", player["_id"]))
                .Sort(new BsonDocument("CreatedAtUtc", 1)).FirstOrDefaultAsync(cancellationToken);
            usedAt = firstPost is null ? null : ReadTime(firstPost.GetValue("CreatedAtUtc", BsonNull.Value));
        }

        var invite = new Invitation(
            InvitationId.From("legacy_" + player["_id"].AsString),
            PlayerId.From(inviterId.AsString),
            PlayerId.From(player["_id"].AsString),
            created.Value,
            true,
            inviteeUsed,
            null,
            usedAt);
        var document = invite.ToBsonDocument();
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
        IEnumerable<Invitation> candidates,
        CancellationToken cancellationToken)
    {
        foreach (var invite in candidates)
        {
            var updated = invite with { InviterBonusUsed = true, InviterBonusUsedAtUtc = invite.CreatedAtUtc };
            await invites.UpdateOneAsync(
                session,
                new BsonDocument("_id", invite.Id.Value),
                new BsonDocument("$set", new BsonDocument
                {
                    ["InviterBonusUsed"] = true,
                    ["InviterBonusUsedAtUtc"] = updated.ToBsonDocument()["InviterBonusUsedAtUtc"],
                }),
                cancellationToken: cancellationToken);
        }
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
