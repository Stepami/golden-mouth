using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class RepairLegacyDocuments : RekiMigrationBase
{
    // Freeze the 2.0.1 skill thresholds so later balance changes cannot alter this migration.
    private static readonly double[] SkillThresholds = [0, 8, 20, 36, 56, 80, 110, 150, 200, 270];
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
        await RepairSkillExperienceAsync(db, session, cancellationToken);
    }

    public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Repaired legacy values cannot be distinguished from original values.");

    private static async Task RepairSkillExperienceAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        using var cursor = await players.FindAsync(
            session, Builders<BsonDocument>.Filter.Type("Skills", BsonType.Document), cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            await RepairSkillBatchAsync(players, session, cursor.Current, cancellationToken);
        }
    }

    private static async Task RepairSkillBatchAsync(
        IMongoCollection<BsonDocument> players,
        IClientSessionHandle session,
        IEnumerable<BsonDocument> documents,
        CancellationToken cancellationToken)
    {
        foreach (var player in documents)
        {
            var updates = CreateSkillUpdates(player["Skills"].AsBsonDocument);
            if (updates.Count > 0)
            {
                await players.UpdateOneAsync(
                    session,
                    Builders<BsonDocument>.Filter.Eq("_id", player["_id"]),
                    Builders<BsonDocument>.Update.Combine(updates),
                    cancellationToken: cancellationToken);
            }
        }
    }

    private static List<UpdateDefinition<BsonDocument>> CreateSkillUpdates(BsonDocument skills)
    {
        var updates = new List<UpdateDefinition<BsonDocument>>();
        foreach (var element in skills.Elements)
        {
            var skill = element.Value.AsBsonDocument;
            var level = skill.GetValue("Level", 1).ToInt32();
            var experience = skill.GetValue("Experience", BsonNull.Value);
            if (!experience.IsBsonNull && (experience.ToDouble() > 0 || level == 1))
            {
                continue;
            }

            var index = Math.Clamp(level - 1, 0, SkillThresholds.Length - 1);
            var progress = Math.Clamp(skill.GetValue("Progress", 0).ToDouble(), 0, 1);
            var inferred = index == SkillThresholds.Length - 1
                ? SkillThresholds[index]
                : SkillThresholds[index] + ((SkillThresholds[index + 1] - SkillThresholds[index]) * progress);
            updates.Add(Builders<BsonDocument>.Update.Set($"Skills.{element.Name}.Experience", inferred));
        }

        return updates;
    }

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
