using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class ConsolidateWorldSchema : RekiMigrationBase
{
    private static readonly string[] WorldFields = ["Name", "Handle", "Role", "Avatar", "Subscribers", "Wants", "Achievements", "LastPostAtUtc", "RestedPostsRemaining", "TotalPostsPublished", "PostsSinceViral", "ConsecutiveFlops"];

    public override DatabaseVersion Version => new(2, 0, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        var worlds = db.GetCollection<BsonDocument>(MongoCollections.Worlds);
        await DropIndexAsync(players, "ix_players_world", cancellationToken);
        await DropIndexAsync(worlds, "ux_worlds_player_world", cancellationToken);
        await CreateWorldIndexesAsync(db, cancellationToken);
        var templates = (await db.GetCollection<BsonDocument>(MongoCollections.WorldTemplates)
            .Find(session, FilterDefinition<BsonDocument>.Empty).ToListAsync(cancellationToken))
            .Select(FlattenTemplate).ToDictionary(template => template["BalanceKey"].AsString, StringComparer.Ordinal);
        using var cursor = await players.FindAsync(session, FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            await MigrateBatchAsync(db, session, cursor.Current, templates, cancellationToken);
        }
    }

    private static async Task CreateWorldIndexesAsync(IMongoDatabase db, CancellationToken cancellationToken)
    {
        var worlds = db.GetCollection<BsonDocument>(MongoCollections.Worlds);
        await worlds.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("PlayerId"), new CreateIndexOptions { Name = "ix_worlds_player" }),
            cancellationToken: cancellationToken);
        var memories = db.GetCollection<BsonDocument>("stars_memory");
        await DropIndexAsync(memories, "ux_stars_memory_player_star", cancellationToken);
        await CreateIndexAsync(memories, "ux_stars_memory_world_star", "StarId", true, cancellationToken);
        await CreateIndexAsync(db.GetCollection<BsonDocument>(MongoCollections.Posts), "ix_posts_world_created", "CreatedAtUtc", false, cancellationToken);
        await CreateIndexAsync(db.GetCollection<BsonDocument>(MongoCollections.Events), "ix_events_world_day", "DayStartedAtUtc", false, cancellationToken);
    }

    private static Task<string> CreateIndexAsync(IMongoCollection<BsonDocument> collection, string name, string field, bool unique, CancellationToken cancellationToken) =>
        collection.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("PlayerId").Ascending("WorldId").Ascending(field),
                new CreateIndexOptions { Name = name, Unique = unique }),
            cancellationToken: cancellationToken);

    private static async Task MigrateBatchAsync(IMongoDatabase db, IClientSessionHandle session, IEnumerable<BsonDocument> players, IReadOnlyDictionary<string, BsonDocument> templates, CancellationToken cancellationToken)
    {
        foreach (var player in players)
        {
            await MigratePlayerAsync(db, session, player, templates, cancellationToken);
        }
    }

    private static async Task MigratePlayerAsync(IMongoDatabase db, IClientSessionHandle session, BsonDocument player, IReadOnlyDictionary<string, BsonDocument> templates, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        var worlds = db.GetCollection<BsonDocument>(MongoCollections.Worlds);
        if (!player.TryGetValue("World", out var kind))
        {
            return;
        }

        var playerId = player["_id"];
        var worldId = new BsonString($"{playerId.AsString}-{kind.AsString}");
        if (!templates.TryGetValue(kind.AsString, out var template))
        {
            return;
        }

        var world = template.DeepClone().AsBsonDocument;
        world["_id"] = worldId;
        world["PlayerId"] = playerId;
        world["Version"] = 0;
        foreach (var field in WorldFields)
        {
            if (player.TryGetValue(field, out var value))
            {
                world[field] = value.DeepClone();
            }
        }

        if (!player.Contains("Avatar") && player.TryGetValue("Handle", out var handle))
        {
            world["Avatar"] = handle.DeepClone();
        }

        var achievements = world.GetValue("Achievements", new BsonArray()).AsBsonArray;
        world["Achievements"] = new BsonArray(achievements.Select(value => value.AsBsonDocument).Where(value => !value.TryGetValue("WorldId", out var previous) || previous == kind).Select(MigrateAchievement));
        var settings = player.GetValue("Settings", new BsonDocument()).AsBsonDocument;
        world["PeriodStartedAtUtc"] = player.GetValue("PostsPeriodStartedAtUtc", new BsonArray { DateTimeOffset.UnixEpoch.Ticks, 0 });
        world["Drama"] = settings.GetValue("Drama", world["Drama"]);
        world["Voice"] = settings.GetValue("Voice", world["Voice"]);
        var relations = player.GetValue("StarRelations", new BsonArray()).AsBsonArray;
        world["Stars"] = new BsonArray(world["Stars"].AsBsonArray.Select(value =>
            MigrateRelation(BsonSerializer.Deserialize<WorldStar>(value.AsBsonDocument), relations).ToBsonDocument()));
        await worlds.ReplaceOneAsync(session, new BsonDocument("_id", worldId), world, new ReplaceOptions { IsUpsert = true }, cancellationToken);

        foreach (var collection in new[] { MongoCollections.Posts, MongoCollections.Publications, "stars_memory", MongoCollections.Events })
        {
            await db.GetCollection<BsonDocument>(collection).UpdateManyAsync(
                session,
                new BsonDocument { { "PlayerId", playerId }, { "WorldId", BsonNull.Value } },
                Builders<BsonDocument>.Update.Set("WorldId", worldId),
                cancellationToken: cancellationToken);
        }

        var unset = new BsonDocument();
        foreach (var field in WorldFields)
        {
            unset[field] = 1;
        }

        unset["World"] = 1;
        unset["StarRelations"] = 1;
        unset["Settings.Drama"] = 1;
        unset["Settings.Voice"] = 1;
        await players.UpdateOneAsync(session, new BsonDocument("_id", playerId), new BsonDocument("$unset", unset), cancellationToken: cancellationToken);
    }

    private static BsonDocument FlattenTemplate(BsonDocument template)
    {
        var flattened = template.DeepClone().AsBsonDocument;
        foreach (var name in new[] { "Interface", "Player" })
        {
            if (flattened.TryGetValue(name, out var value) && value.IsBsonDocument)
            {
                CopyTemplateFields(flattened, value.AsBsonDocument);

                flattened.Remove(name);
            }
        }

        return flattened;
    }

    private static void CopyTemplateFields(BsonDocument target, BsonDocument source)
    {
        foreach (var field in source.Elements)
        {
            target[field.Name] = field.Value.DeepClone();
        }
    }

    private static BsonDocument MigrateAchievement(BsonDocument achievement)
    {
        var migrated = achievement.DeepClone().AsBsonDocument;
        migrated.Remove("WorldId");
        return migrated;
    }

    private static WorldStar MigrateRelation(WorldStar star, BsonArray relations)
    {
        var relation = relations.Select(value => value.AsBsonDocument)
            .FirstOrDefault(value => value.GetValue("StarId", string.Empty) == star.Id.Value);
        if (relation is null)
        {
            return star;
        }

        var persisted = star.ToBsonDocument();
        persisted["Relation"] = relation.GetValue("Value", 0);
        persisted["RelationGainedInPeriod"] = relation.GetValue("GainedInPeriod", 0);
        persisted["RelationPeriodStartedAtUtc"] = relation.GetValue("PeriodStartedAtUtc", persisted["RelationPeriodStartedAtUtc"]);
        return BsonSerializer.Deserialize<WorldStar>(persisted);
    }

    private static async Task DropIndexAsync(IMongoCollection<BsonDocument> collection, string name, CancellationToken cancellationToken)
    {
        using var indexes = await collection.Indexes.ListAsync(cancellationToken);
        if ((await indexes.ToListAsync(cancellationToken)).Any(index => index["name"] == name))
        {
            await collection.Indexes.DropOneAsync(name, cancellationToken);
        }
    }
}
