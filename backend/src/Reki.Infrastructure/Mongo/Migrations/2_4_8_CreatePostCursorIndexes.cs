using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Posts;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class CreatePostCursorIndexes : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 8);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var posts = db.GetCollection<GamePost>(MongoCollections.Posts);
        var keys = Builders<GamePost>.IndexKeys;
        await posts.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<GamePost>(
                keys.Ascending("WorldId").Descending("_id"),
                new CreateIndexOptions { Name = "ix_posts_world_id" }),
            new CreateIndexModel<GamePost>(
                keys.Ascending("WorldId").Ascending("AuthorId").Descending("_id"),
                new CreateIndexOptions { Name = "ix_posts_world_author_id" }),
        ],
        cancellationToken);

        foreach (var (collectionName, indexName) in new[]
        {
            (MongoCollections.Posts, "ix_posts_world_created"),
            (MongoCollections.Invites, "ix_invites_player"),
        })
        {
            var collection = db.GetCollection<BsonDocument>(collectionName);
            using var cursor = await collection.Indexes.ListAsync(cancellationToken);
            if ((await cursor.ToListAsync(cancellationToken)).Any(index => index["name"] == indexName))
            {
                await collection.Indexes.DropOneAsync(indexName, cancellationToken);
            }
        }
    }
}
