using Kot.MongoDB.Migrations;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class RemoveUnusedThreads : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 4);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        // MongoDB cannot drop a collection inside a transaction. The drop is safe to retry
        // if the process stops before the migrator records this version.
        try
        {
            await db.DropCollectionAsync("threads", cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 26)
        {
            // NamespaceNotFound: this database has no legacy collection to remove.
        }
    }
}
