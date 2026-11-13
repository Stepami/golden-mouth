using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Posts;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddPostLikes : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 7, 4);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var posts = db.GetCollection<BsonDocument>(MongoCollections.Posts);
        var missing = Builders<BsonDocument>.Filter.Exists("Likes", false);
        using var cursor = await posts.FindAsync(session, missing, cancellationToken: cancellationToken);
        await foreach (var post in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", post["_id"]) & missing;
            var likes = GamePost.CalculateLikes(post["Views"].ToInt64(), Random.Shared.Next(2, 11));
            var update = Builders<BsonDocument>.Update.Set("Likes", likes).Set("LikedBy", new BsonArray()).Unset("ReactionEmoji");
            await posts.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken);
        }
    }
}
