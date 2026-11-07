using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddPostEmbeddingFilters : RekiMigrationBase
{
    private const string IndexName = "ix_posts_embedding";

    public override DatabaseVersion Version => new(2, 6, 2);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var posts = db.GetCollection<BsonDocument>(MongoCollections.Posts);
        var definition = new BsonDocument("fields", new BsonArray
        {
            new BsonDocument { { "type", "vector" }, { "path", "_embedding" }, { "numDimensions", 312 }, { "similarity", "cosine" } },
            new BsonDocument { { "type", "filter" }, { "path", "WorldId" } },
            new BsonDocument { { "type", "filter" }, { "path", "AuthorId" } },
        });
        using var indexes = await posts.SearchIndexes.ListAsync(cancellationToken: cancellationToken);
        var previous = (await indexes.ToListAsync(cancellationToken)).First(index => index["name"] == IndexName);
        var version = previous["latestDefinitionVersion"]["version"].ToInt32();
        await posts.SearchIndexes.UpdateAsync(IndexName, definition, cancellationToken);

        // An updated index can remain queryable using its previous definition while rebuilding.
        while (true)
        {
            using var currentIndexes = await posts.SearchIndexes.ListAsync(cancellationToken: cancellationToken);
            var current = (await currentIndexes.ToListAsync(cancellationToken)).First(index => index["name"] == IndexName);
            if (current["latestDefinitionVersion"]["version"].ToInt32() > version && current["status"] == "READY")
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
}
