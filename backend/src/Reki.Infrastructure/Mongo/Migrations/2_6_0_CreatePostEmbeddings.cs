using Kot.MongoDB.Migrations;
using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Reki.Application.Posts.AI;
using Reki.Domain.Posts;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class CreatePostEmbeddings(
    IEncodeSerializer<GamePost> serializer,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : RekiMigrationBase
{
    private const string IndexName = "ix_posts_embedding";

    public override DatabaseVersion Version => new(2, 6, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var posts = db.GetCollection<GamePost>(MongoCollections.Posts);
        using var indexes = await posts.SearchIndexes.ListAsync(cancellationToken: cancellationToken);
        if (!(await indexes.ToListAsync(cancellationToken)).Any(index => index["name"] == IndexName))
        {
            await posts.SearchIndexes.CreateOneAsync(
                new CreateVectorSearchIndexModel<GamePost>("embedding", IndexName, VectorSimilarity.Cosine, 312),
                cancellationToken);
        }

        // Persist each embedding outside the migration transaction so a long backfill can resume.
        var documents = db.GetCollection<BsonDocument>(MongoCollections.Posts);
        var missing = Builders<GamePost>.Filter.Exists("embedding", false);
        await foreach (var post in posts.AsQueryable().Where(_ => missing.Inject()).ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var embedding = await embeddingGenerator.GenerateVectorAsync(
                serializer.Serialize(post), cancellationToken: cancellationToken);
            await documents.UpdateOneAsync(
                new BsonDocument("_id", new BsonBinaryData(post.Id.Value, GuidRepresentation.Standard)),
                Builders<BsonDocument>.Update.Set("embedding", new BsonArray(embedding.ToArray())),
                cancellationToken: cancellationToken);
        }
    }
}
