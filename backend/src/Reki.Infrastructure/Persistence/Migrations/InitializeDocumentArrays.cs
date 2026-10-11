using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class InitializeDocumentArrays : RekiMigrationBase
{
    private static readonly string[] WorldArrays =
        ["Roles", "Voices", "Presets", "PostIdeas", "DemoPosts", "Rules", "Stars", "Wants", "Achievements"];

    public override DatabaseVersion Version => new(2, 0, 1);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        await InitializeArraysAsync(db, session, MongoCollections.Posts, ["Comments", "ThreadReplies"], cancellationToken);
        await InitializeArraysAsync(db, session, MongoCollections.StarsMemory, ["Memory", "Messages"], cancellationToken);
        await InitializeArraysAsync(db, session, MongoCollections.Events, ["Ideas"], cancellationToken);
        await InitializeArraysAsync(db, session, MongoCollections.Publications, ["Parts"], cancellationToken);
        await InitializeArraysAsync(db, session, MongoCollections.Worlds, WorldArrays, cancellationToken);
        await InitializeArraysAsync(db, session, MongoCollections.WorldTemplates, WorldArrays, cancellationToken);
    }

    public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Initialized arrays cannot be distinguished from existing empty arrays.");

    private static async Task InitializeArraysAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        string collectionName,
        IReadOnlyList<string> fields,
        CancellationToken cancellationToken)
    {
        var collection = db.GetCollection<BsonDocument>(collectionName);
        var filters = Builders<BsonDocument>.Filter;
        foreach (var fieldName in fields)
        {
            var missingOrNull = filters.And(
                filters.Or(filters.Exists(fieldName, false), filters.Type(fieldName, BsonType.Null)),
                filters.Not(filters.Type(fieldName, BsonType.Array)));
            await collection.UpdateManyAsync(
                session,
                missingOrNull,
                Builders<BsonDocument>.Update.Set(fieldName, new BsonArray()),
                cancellationToken: cancellationToken);
        }
    }
}
