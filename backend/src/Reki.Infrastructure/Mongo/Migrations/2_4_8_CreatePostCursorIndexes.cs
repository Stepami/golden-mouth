using Kot.MongoDB.Migrations;
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
                keys.Ascending(post => post.WorldId).Descending(post => post.Id),
                new CreateIndexOptions { Name = "ix_posts_world_id" }),
            new CreateIndexModel<GamePost>(
                keys.Ascending(post => post.WorldId).Ascending(post => post.AuthorId).Descending(post => post.Id),
                new CreateIndexOptions { Name = "ix_posts_world_author_id" }),
        ],
        cancellationToken);

        using var cursor = await posts.Indexes.ListAsync(cancellationToken);
        if ((await cursor.ToListAsync(cancellationToken)).Any(index => index["name"] == "ix_posts_world_created"))
        {
            await posts.Indexes.DropOneAsync("ix_posts_world_created", cancellationToken);
        }
    }
}
