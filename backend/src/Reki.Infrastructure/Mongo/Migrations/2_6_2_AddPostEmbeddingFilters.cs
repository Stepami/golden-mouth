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

        await posts.SearchIndexes.UpdateAsync(IndexName, definition, cancellationToken);
    }
}
