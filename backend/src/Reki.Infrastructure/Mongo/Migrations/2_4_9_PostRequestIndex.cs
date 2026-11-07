using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Posts;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class PostRequestIndex : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 4, 9);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var posts = db.GetCollection<GamePost>(MongoCollections.Posts);
        var keys = Builders<GamePost>.IndexKeys.Ascending(post => post.WorldId).Ascending(post => post.AuthorId).Ascending(post => post.RequestId);
        var filter = new BsonDocument("RequestId", new BsonDocument("$type", "string"));
        await posts.Indexes.CreateOneAsync(new CreateIndexModel<GamePost>(keys, new CreateIndexOptions<GamePost> { Name = "ux_posts_request", Unique = true, PartialFilterExpression = filter }), cancellationToken: cancellationToken);
    }
}
