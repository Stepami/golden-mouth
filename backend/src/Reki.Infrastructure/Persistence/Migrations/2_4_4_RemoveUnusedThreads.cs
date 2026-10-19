using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class RemoveUnusedThreads : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 4);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var options = new ListCollectionNamesOptions { Filter = new BsonDocument("name", "threads") };
        using var collections = await db.ListCollectionNamesAsync(options, cancellationToken);
        if (await collections.AnyAsync(cancellationToken))
        {
            // MongoDB cannot drop a collection inside a transaction. The drop is safe to retry
            // if the process stops before the migrator records this version.
            await db.DropCollectionAsync("threads", cancellationToken);
        }
    }
}
