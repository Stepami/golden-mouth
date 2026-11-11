using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddPlayerLastSeenAt(TimeProvider timeProvider) : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 7, 1);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        var posts = db.GetCollection<BsonDocument>(MongoCollections.Posts);
        var missing = Builders<BsonDocument>.Filter.Exists("LastSeenAt", false);
        var fallback = WriteTime(timeProvider.GetUtcNow().AddDays(-7));
        using var cursor = await players.FindAsync(session, missing, cancellationToken: cancellationToken);
        await foreach (var player in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var id = player["_id"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard);
            var post = await posts.Find(session, new BsonDocument("AuthorId", $"Player_{id:N}"))
                .Sort(new BsonDocument("_id", -1))
                .FirstOrDefaultAsync(cancellationToken);
            var lastSeen = post is not null
                ? post["CreatedAtUtc"]
                : player.GetValue("CreatedAtUtc", fallback);
            await players.UpdateOneAsync(
                session,
                new BsonDocument("_id", player["_id"]),
                Builders<BsonDocument>.Update.Set("LastSeenAt", lastSeen),
                cancellationToken: cancellationToken);
        }
    }

    private static BsonValue WriteTime(DateTimeOffset value)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        writer.WriteStartDocument();
        writer.WriteName("value");
        BsonSerializer.LookupSerializer<DateTimeOffset>().Serialize(BsonSerializationContext.CreateRoot(writer), value);
        writer.WriteEndDocument();
        return document["value"];
    }
}
