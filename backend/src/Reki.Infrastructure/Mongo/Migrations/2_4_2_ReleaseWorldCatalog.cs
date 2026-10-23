using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

/// <summary>Updates deployments that already passed the earlier DEV migration versions.</summary>
internal sealed partial class ReleaseWorldCatalog : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 2);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var templates = db.GetCollection<World>(MongoCollections.WorldTemplates);
        await templates.DeleteManyAsync(session, Builders<World>.Filter.Ne(item => item.Id, WorldId.From("it")), cancellationToken: cancellationToken);
        await templates.InsertManyAsync(session, CreateReleaseTemplates(), cancellationToken: cancellationToken);
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        await players.UpdateManyAsync(
            session,
            new BsonDocument("HitOwnWorldMilestone", BsonNull.Value),
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
