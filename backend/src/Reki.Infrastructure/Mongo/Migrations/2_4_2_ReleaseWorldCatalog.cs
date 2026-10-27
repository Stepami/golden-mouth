using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

/// <summary>Updates deployments that already passed the earlier DEV migration versions.</summary>
internal sealed partial class ReleaseWorldCatalog : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 2);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var templates = db.GetCollection<BsonDocument>(MongoCollections.WorldTemplates);
        var keep = new BsonArray { "it", new BsonBinaryData(Guid.Parse("5720946a-ea7c-4a10-9c50-51ce3deefc53"), GuidRepresentation.Standard) };
        await templates.DeleteManyAsync(session, new BsonDocument("_id", new BsonDocument("$nin", keep)), cancellationToken: cancellationToken);
        foreach (var template in CreateReleaseTemplates())
        {
            var document = template.ToBsonDocument();
            document["BalanceKey"] = template.Id.Value.ToString("D") switch
            {
                "b3a1a522-0263-4796-a197-57a8731fc291" => "evening",
                "aec9494b-4118-4525-8458-e85d759c8dd9" => "sorcerers",
                "8c02a7fb-3b55-4eb0-b957-1b4869ed9834" => "houses",
                _ => throw new InvalidOperationException("Unknown release template."),
            };
            await templates.ReplaceOneAsync(session, new BsonDocument("_id", document["_id"]), document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }

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
