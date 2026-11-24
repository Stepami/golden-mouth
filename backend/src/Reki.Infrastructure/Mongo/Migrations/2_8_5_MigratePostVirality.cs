using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Bonuses;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class MigratePostVirality : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 5);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var posts = db.GetCollection<BsonDocument>(MongoCollections.Posts);
        var filters = Builders<BsonDocument>.Filter;
        var updates = Builders<BsonDocument>.Update;
        var missingVirality = filters.Exists(nameof(GamePost.Virality), false);

        foreach (var (filter, status) in new[]
                 {
                     (filters.Eq("Viral", true), PostViralityStatus.Viral),
                     (filters.Eq("Flop", true), PostViralityStatus.Flop),
                     (FilterDefinition<BsonDocument>.Empty, PostViralityStatus.None),
                 })
        {
            await posts.UpdateManyAsync(
                session,
                missingVirality & filter,
                updates.Set(nameof(GamePost.Virality), (int)status).Unset("Viral").Unset("Flop"),
                cancellationToken: cancellationToken);
        }

        await SettleInvitationsAsync(db, session, cancellationToken);
    }

    private static async Task SettleInvitationsAsync(
        IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var invitations = db.GetCollection<BsonDocument>(MongoCollections.Invites);
        var players = db.GetCollection<PlayerProfile>(MongoCollections.Players);
        var bonuses = db.GetCollection<Bonus>(MongoCollections.Bonuses);
        var pending = await invitations.Find(session, FilterDefinition<BsonDocument>.Empty)
            .ToListAsync(cancellationToken);

        foreach (var invitation in pending)
        {
            var inviterFilter = new BsonDocument("_id", invitation["InviterId"]);
            var inviteeFilter = new BsonDocument("_id", invitation["InviteeId"]);
            var inviter = await players.Find(session, inviterFilter).FirstOrDefaultAsync(cancellationToken);
            var invitee = await players.Find(session, inviteeFilter).FirstOrDefaultAsync(cancellationToken);
            if (inviter is null || invitee is null)
            {
                continue;
            }

            if (!invitation.GetValue("InviterBonusUsed", false).ToBoolean())
            {
                inviter.AwardReferral();
                await players.ReplaceOneAsync(session, inviterFilter, inviter, cancellationToken: cancellationToken);
                await bonuses.InsertManyAsync(
                    session,
                    [
                    new ViralCardBonus(BonusId.New(), inviter.Id, 5),
                    new ViralCardBonus(BonusId.New(), inviter.Id, 5),
                    new ViralCardBonus(BonusId.New(), inviter.Id, 5),
                    new FlopGuardBonus(BonusId.New(), inviter.Id, 10),
                    new FlopGuardBonus(BonusId.New(), inviter.Id, 10),
                    new FlopGuardBonus(BonusId.New(), inviter.Id, 10),
                    ],
                    cancellationToken: cancellationToken);
            }

            if (!invitation.GetValue("InviteeBonusUsed", false).ToBoolean())
            {
                // Reload to preserve the inviter award if both IDs happen to match.
                invitee = await players.Find(session, inviteeFilter).FirstAsync(cancellationToken);
                invitee.ReplenishCrystals(invitation["BonusAmount"].ToInt32());
                await players.ReplaceOneAsync(session, inviteeFilter, invitee, cancellationToken: cancellationToken);
            }
        }

        await invitations.DeleteManyAsync(session, FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
    }
}
