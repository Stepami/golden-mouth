using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class RemoveUnusedSocialCollections : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 7);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        foreach (var name in new[] { "stars_memory", "challenges" })
        {
            var options = new ListCollectionNamesOptions { Filter = new BsonDocument("name", name) };
            using var collections = await db.ListCollectionNamesAsync(options, cancellationToken);
            if (await collections.AnyAsync(cancellationToken))
            {
                // MongoDB cannot drop collections inside a transaction. Each drop is safe to retry
                // if the process stops before the migrator records this version.
                await db.DropCollectionAsync(name, cancellationToken);
            }
        }
    }
}
