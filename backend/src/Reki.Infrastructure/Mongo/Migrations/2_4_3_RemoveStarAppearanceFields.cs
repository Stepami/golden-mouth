using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class RemoveStarAppearanceFields : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 3);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        await UpdateCatalogAvatarsAsync(db.GetCollection<BsonDocument>(MongoCollections.WorldTemplates), session, cancellationToken);
        await UpdateCatalogAvatarsAsync(db.GetCollection<BsonDocument>(MongoCollections.Worlds), session, cancellationToken);

        var update = Builders<BsonDocument>.Update.Pipeline(new[]
        {
            new BsonDocument("$unset", new BsonArray
            {
                "Stars.AppearanceProfile",
                "Stars.AgeBand",
                "ArchivedStars",
            }),
        });
        await db.GetCollection<BsonDocument>(MongoCollections.WorldTemplates).UpdateManyAsync(
            session,
            FilterDefinition<BsonDocument>.Empty,
            update,
            cancellationToken: cancellationToken);
        await db.GetCollection<BsonDocument>(MongoCollections.Worlds).UpdateManyAsync(
            session,
            FilterDefinition<BsonDocument>.Empty,
            update,
            cancellationToken: cancellationToken);
    }

    private static async Task UpdateCatalogAvatarsAsync(
        IMongoCollection<BsonDocument> collection,
        IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        (string World, string StarId, string AvatarSeed)[] replacements =
        [
            ("evening", "eduard", "ed_thekiller_skin"),
            ("evening", "yakov", "nessssssie"),
            ("sorcerers", "gosha", "gosh"),
            ("sorcerers", "georgina", "__curly_wit_"),
            ("houses", "diana", "dracary"),
            ("houses", "jon", "__ice__and__fire__"),
            ("houses", "james", "_gold__lion"),
        ];
        foreach (var replacement in replacements)
        {
            await collection.UpdateManyAsync(
                session,
                new BsonDocument
                {
                    { "BalanceKey", replacement.World },
                    { "Stars", new BsonDocument("$elemMatch", new BsonDocument { { "_id", replacement.StarId }, { "AvatarSeed", replacement.StarId } }) },
                },
                Builders<BsonDocument>.Update.Set("Stars.$.AvatarSeed", replacement.AvatarSeed),
                cancellationToken: cancellationToken);
        }

        await collection.UpdateManyAsync(
            session,
            new BsonDocument { { "BalanceKey", "houses" }, { "Interface.AvatarStyle", "thumbs" } },
            Builders<BsonDocument>.Update.Set("Interface.AvatarStyle", "adventurer"),
            cancellationToken: cancellationToken);
    }
}
