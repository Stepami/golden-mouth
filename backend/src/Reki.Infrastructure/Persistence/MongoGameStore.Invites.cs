using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Persistence;

internal sealed partial class MongoGameStore
{
    public async ValueTask<InviteState> GetInviteStateAsync(PlayerId playerId, DateTimeOffset periodStart, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Eq("InviterId", playerId.Value),
            Builders<BsonDocument>.Filter.Eq("InviteeId", playerId.Value));
        var invitations = await _invites.Find(filter).ToListAsync(cancellationToken);
        foreach (var invitation in invitations.Where(item => item.Contains("InviteeId")))
        {
            await RecoverInviteBonusAsync(invitation, cancellationToken);
        }

        // Reload after repairs, including another request's atomic first-post update.
        invitations = await _invites.Find(filter).ToListAsync(cancellationToken);
        var own = invitations.SingleOrDefault(item => item.GetValue("InviteeId", BsonNull.Value) == playerId.Value);
        var inviterBonuses = invitations.Count(item =>
            item.GetValue("InviterId", BsonNull.Value) == playerId.Value &&
            InPeriod(item, "InviterBonusAtUtc", periodStart));
        var legacyBonuses = invitations.Where(item =>
                item.GetValue("InviterId", BsonNull.Value) == playerId.Value &&
                InPeriod(item, "LegacyPeriodAtUtc", periodStart))
            .Sum(item => item.GetValue("LegacyInviterBonuses", 0).AsInt32);
        var inviteePosts = own is not null && InPeriod(own, "InviteeBonusAtUtc", periodStart)
            ? own["BonusPosts"].AsInt32
            : 0;
        return new InviteState(
            inviterBonuses,
            legacyBonuses,
            inviteePosts,
            own is not null && !own["InviteeBonusUsed"].AsBoolean ? own["BonusPosts"].AsInt32 : 0,
            own is not null);
    }

    private static bool InPeriod(BsonDocument invitation, string field, DateTimeOffset start) =>
        invitation.TryGetValue(field, out var value) && value.IsBsonDateTime &&
        value.AsBsonDateTime.ToUniversalTime() >= start.UtcDateTime &&
        value.AsBsonDateTime.ToUniversalTime() < start.AddDays(1).UtcDateTime;

    private static BsonDocument CreateInviteDocument(PlayerProfile player, PlayerId inviter, DateTimeOffset createdAtUtc, int bonus) => new()
    {
        ["_id"] = player.Id.Value,
        ["InviterId"] = inviter.Value,
        ["InviteeId"] = player.Id.Value,
        ["CreatedAtUtc"] = createdAtUtc.UtcDateTime,
        ["BonusPosts"] = bonus,
        ["InviterBonusUsed"] = false,
        ["InviteeBonusUsed"] = false,

        // A durable registration intent bridges the two writes without a transaction.
        // It is removed once the profile exists and is recovered on the next read.
        ["PendingPlayer"] = player.ToBsonDocument(),
        ["PendingTelegramUserId"] = player.TelegramUserId.Value,
    };

    private async Task RecoverOnboardingAsync(BsonDocument filter, CancellationToken cancellationToken)
    {
        var pending = await _invites
            .Find(Builders<BsonDocument>.Filter.And(filter, Builders<BsonDocument>.Filter.Exists("PendingPlayer")))
            .ToListAsync(cancellationToken);
        foreach (var invitation in pending)
        {
            var player = BsonSerializer.Deserialize<PlayerProfile>(invitation["PendingPlayer"].AsBsonDocument);
            try
            {
                await _players.InsertOneAsync(player, cancellationToken: cancellationToken);
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category ==
                                                        ServerErrorCategory.DuplicateKey)
            {
                var saved = await _players.Find(item => item.TelegramUserId == player.TelegramUserId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (saved?.Id != player.Id)
                {
                    await _invites.DeleteOneAsync(new BsonDocument("_id", invitation["_id"]), cancellationToken);
                    continue;
                }
            }

            await _invites.UpdateOneAsync(
                new BsonDocument("_id", invitation["_id"]),
                Builders<BsonDocument>.Update.Unset("PendingPlayer").Unset("PendingTelegramUserId"),
                cancellationToken: cancellationToken);
        }
    }

    private async Task RecoverInviteBonusAsync(BsonDocument invitation, CancellationToken cancellationToken)
    {
        if (invitation["InviterBonusUsed"].AsBoolean && invitation["InviteeBonusUsed"].AsBoolean)
        {
            return;
        }

        var invitee = PlayerId.From(invitation["InviteeId"].AsString);

        // Only a committed post is evidence of success. Failed generation/moderation
        // never creates one. This also repairs a crash between publication and this update.
        var firstPost = await _posts.Find(item => item.PlayerId == invitee).SortBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .Project(item => new { item.Id, item.CreatedAtUtc }).FirstOrDefaultAsync(cancellationToken);
        if (firstPost is null)
        {
            return;
        }

        foreach (var side in new[] { "Inviter", "Invitee" })
        {
            var filter = new BsonDocument { { "_id", invitation["_id"] }, { $"{side}BonusUsed", false } };
            var update = Builders<BsonDocument>.Update.Set($"{side}BonusUsed", true)
                .Set($"{side}BonusAtUtc", firstPost.CreatedAtUtc.UtcDateTime)
                .Set($"{side}BonusPostId", firstPost.Id.Value);
            await _invites.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
    }
}