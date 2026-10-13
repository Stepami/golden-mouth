using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class UnlockOwnWorld : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 2, 2);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        await players.UpdateManyAsync(
            session,
            FilterDefinition<BsonDocument>.Empty,
            new BsonDocument("$set", new BsonDocument("HitOwnWorldMilestone", false)),
            cancellationToken: cancellationToken);
        var worlds = await db.GetCollection<BsonDocument>(MongoCollections.Worlds)
            .Find(session, new BsonDocument("Player.Subscribers", new BsonDocument("$gte", 500))).ToListAsync(cancellationToken);
        var invites = await db.GetCollection<BsonDocument>(MongoCollections.Invites)
            .Find(session, new BsonDocument("InviterBonusUsed", true)).ToListAsync(cancellationToken);
        var owners = worlds.Select(world => world["Player"]["PlayerId"]).Concat(invites.Select(invite => invite["InviterId"])).Distinct();
        foreach (var owner in owners)
        {
            await players.UpdateOneAsync(
                session,
                new BsonDocument("_id", owner),
                new BsonDocument("$set", new BsonDocument("HitOwnWorldMilestone", true)),
                cancellationToken: cancellationToken);
        }
    }
}
