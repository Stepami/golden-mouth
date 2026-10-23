using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class NestWorldDocuments : RekiMigrationBase
{
    private static readonly string[] InterfaceFields =
        ["Title", "Description", "AvatarStyle", "Where", "FirstContext", "FirstPrompt", "Emoji", "Summary", "Difficulty", "UnlockSkill", "UnlockLevel"];

    private static readonly string[] PlayerFields =
        ["PlayerId", "Voice", "Drama", "Name", "Role", "Avatar", "Subscribers", "Wants", "Achievements", "LastPostAtUtc", "RestedPostsRemaining", "PeriodStartedAtUtc"];

    private static readonly string[] RemovedFields =
        ["Handle", "Presets", "TotalPostsPublished", "PostsSinceViral", "ConsecutiveFlops"];

    public override DatabaseVersion Version => new(2, 0, 2);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var worlds = db.GetCollection<BsonDocument>(MongoCollections.Worlds);
        using var cursor = await worlds.Indexes.ListAsync(cancellationToken);
        var index = (await cursor.ToListAsync(cancellationToken)).SingleOrDefault(item => item["name"] == "ix_worlds_player");
        if (index is not null && index["key"] != new BsonDocument("Player.PlayerId", 1))
        {
            await worlds.Indexes.DropOneAsync("ix_worlds_player", cancellationToken);
        }

        await worlds.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(new BsonDocument("Player.PlayerId", 1), new CreateIndexOptions { Name = "ix_worlds_player" }),
            cancellationToken: cancellationToken);
        await MigrateCollectionAsync(db, session, MongoCollections.Worlds, cancellationToken);
        await MigrateCollectionAsync(db, session, MongoCollections.WorldTemplates, cancellationToken);
    }

    private static async Task MigrateCollectionAsync(IMongoDatabase db, IClientSessionHandle session, string name, CancellationToken cancellationToken)
    {
        var collection = db.GetCollection<BsonDocument>(name);
        using var cursor = await collection.FindAsync(session, FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            await MigrateBatchAsync(collection, session, cursor.Current, cancellationToken);
        }
    }

    private static async Task MigrateBatchAsync(IMongoCollection<BsonDocument> collection, IClientSessionHandle session, IEnumerable<BsonDocument> documents, CancellationToken cancellationToken)
    {
        foreach (var document in documents)
        {
            var migrated = MigrateDocument(document);
            await collection.ReplaceOneAsync(session, new BsonDocument("_id", document["_id"]), migrated, cancellationToken: cancellationToken);
        }
    }

    private static BsonDocument MigrateDocument(BsonDocument document)
    {
        var worldInterface = MoveFields(document, "Interface", InterfaceFields);
        var player = MoveFields(document, "Player", PlayerFields);
        if ((!player.TryGetValue("Avatar", out var avatar) || avatar.IsBsonNull || avatar == string.Empty) &&
            document.TryGetValue("Handle", out var handle))
        {
            player["Avatar"] = handle;
        }

        var drama = player.GetValue("Drama", "mid");
        if (drama.IsBsonNull)
        {
            drama = "mid";
        }

        player["Drama"] = drama.IsString ? drama.AsString switch
        {
            "soft" => 0,
            "mid" => 1,
            "hot" => 2,
            "off" => 3,
            _ => 1,
        } : drama;
        if (!worldInterface.TryGetValue("Difficulty", out var difficulty) || difficulty.IsBsonNull || difficulty == 0)
        {
            worldInterface["Difficulty"] = 1;
        }

        document["Interface"] = worldInterface;
        document["Player"] = player;
        RemoveObsoleteFields(document);

        return document;
    }

    private static void RemoveObsoleteFields(BsonDocument document)
    {
        foreach (var field in RemovedFields)
        {
            document.Remove(field);
        }
    }

    private static BsonDocument MoveFields(BsonDocument document, string name, IReadOnlyList<string> fields)
    {
        var nested = document.TryGetValue(name, out var existing) && existing.IsBsonDocument
            ? existing.AsBsonDocument.DeepClone().AsBsonDocument
            : new BsonDocument();
        foreach (var field in fields)
        {
            if (document.TryGetValue(field, out var value))
            {
                if (!nested.Contains(field))
                {
                    nested[field] = value.DeepClone();
                }

                document.Remove(field);
            }
        }

        return nested;
    }
}