using Kot.MongoDB.Migrations;
using MongoDB.Driver;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class ResetTickerQ : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 7, 6);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        using var cursor = await db.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        var collections = await cursor.ToListAsync(cancellationToken);
        var legacyCollections = new[] { "ticker_TimeTickers", "ticker_CronTickers", "ticker_CronTickerOccurrences" }
            .Where(collections.Contains).ToArray();
        foreach (var name in legacyCollections)
        {
            await db.DropCollectionAsync(name, cancellationToken);
        }
    }
}
