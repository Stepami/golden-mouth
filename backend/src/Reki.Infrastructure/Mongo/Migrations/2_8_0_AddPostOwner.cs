using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddPostOwner : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var worlds = db.GetCollection<BsonDocument>(MongoCollections.Worlds);
        var posts = db.GetCollection<BsonDocument>(MongoCollections.Posts);
        await posts.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("OwnerId"), new CreateIndexOptions { Name = "ix_posts_owner" }),
            cancellationToken: cancellationToken);

        var missingOwner = Builders<BsonDocument>.Filter.Exists("OwnerId", false);
        using var cursor = await worlds.FindAsync(session, FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        await foreach (var world in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var filter = missingOwner & Builders<BsonDocument>.Filter.Eq("WorldId", world["_id"]);
            await posts.UpdateManyAsync(
                session,
                filter,
                Builders<BsonDocument>.Update.Set("OwnerId", world["Player"]["PlayerId"]),
                cancellationToken: cancellationToken);
        }

        await posts.DeleteManyAsync(session, missingOwner, cancellationToken: cancellationToken);
    }
}
