using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class RemovePublications : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 6, 1);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var posts = db.GetCollection<BsonDocument>(MongoCollections.Posts);
        var missing = Builders<BsonDocument>.Filter.Exists("ReactionEmoji", false);
        using var cursor = await posts.FindAsync(session, missing, cancellationToken: cancellationToken);
        var emojis = new[] { "🔥", "❤️", "👎", "🤔" };
        await foreach (var post in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", post["_id"]) & missing;
            await posts.UpdateOneAsync(
                session,
                filter,
                Builders<BsonDocument>.Update.Set("ReactionEmoji", emojis[Random.Shared.Next(emojis.Length)]),
                cancellationToken: cancellationToken);
        }

        var options = new ListCollectionNamesOptions { Filter = new BsonDocument("name", "publications") };
        using var collections = await db.ListCollectionNamesAsync(options, cancellationToken);
        if (await collections.AnyAsync(cancellationToken))
        {
            await db.DropCollectionAsync("publications", cancellationToken);
        }
    }
}
