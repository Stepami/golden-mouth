using System.Globalization;
using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reki.Api.Serialization;
using Reki.Application.Balance;
using Reki.Domain.Events;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs.GenerateStarFeedPost;
using Reki.Infrastructure.Mongo.Migrations;
using Reki.Infrastructure.Mongo.Options;
using Reki.Infrastructure.Mongo.Repositories;
using TickerQ.Utilities;

namespace Reki.Tests.Persistence;

[ClassDataSource<TestSessionFixture>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Migrations", Order = 0)]
public sealed class GameIndexMigrationTests(TestSessionFixture mongo)
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 5, 15, 0, 0, 123, TimeSpan.FromHours(3));

    [Test]
    public async Task Migrate_LegacyComments_PreservesParentLinksAsync()
    {
        await using var scope = CreateDatabase();
        var posts = scope.Database.GetCollection<BsonDocument>("posts");
        var original = CreatePost(PostId.New(CreatedAt).Value.ToString("N"), false);
        original["Comments"] = new BsonArray
        {
            new BsonDocument { ["_id"] = "legacy-root", ["ParentCommentId"] = BsonNull.Value, ["Text"] = "root" },
            new BsonDocument { ["_id"] = "legacy-child", ["ParentCommentId"] = "legacy-root", ["Text"] = "reply" },
        };
        await posts.InsertOneAsync(original);

        await MigratePostIdsAsync(scope.Database);

        var migrated = await posts.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        var comments = migrated["Comments"].AsBsonArray;
        await Assert.That(migrated["_id"].AsBsonBinaryData.ToGuid()).IsEqualTo(Guid.Parse(original["_id"].AsString));
        await Assert.That(comments[0]["_id"].AsBsonBinaryData.SubType == BsonBinarySubType.UuidStandard).IsTrue();
        await Assert.That(comments[1]["_id"].AsBsonBinaryData.SubType == BsonBinarySubType.UuidStandard).IsTrue();
        await Assert.That(comments[0]["_id"]).IsNotEqualTo(comments[1]["_id"]);
        await Assert.That(comments[1]["ParentCommentId"]).IsEqualTo(comments[0]["_id"]);
        await Assert.That(comments[1]["Text"]).IsEqualTo("reply");

        await MigratePostIdsAsync(scope.Database, downgrade: true);

        await Assert.That(await posts.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(original);
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, true)]
    public async Task Migrate_PostIds_PreservesReferencesOnRetryAsync(bool bsonDateTime, bool legacyTickerFormat)
    {
        var database = mongo.MongoClient.GetDatabase($"post_ids_{Guid.NewGuid():N}");
        try
        {
            var posts = database.GetCollection<BsonDocument>("posts");
            var publications = database.GetCollection<BsonDocument>("publications");
            var tickers = database.GetCollection<BsonDocument>("ticker_TimeTickers");
            var invitations = database.GetCollection<BsonDocument>("invites");
            await invitations.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("InviteeId", 1), new CreateIndexOptions { Unique = true }));
            var oldInvitation = new BsonDocument { ["_id"] = "legacy-invitation", ["InviteeId"] = "a96fb5ddf0564809a1d2bb5ed78a210f", ["BonusAmount"] = 5 };
            await invitations.InsertOneAsync(oldInvitation);
            const string playerId = "a96fb5ddf0564809a1d2bb5ed78a210f";
            var legacyReferences = new Dictionary<string, BsonDocument>
            {
                ["players"] = new() { ["_id"] = playerId, ["Version"] = 3 },
                ["worlds"] = new() { ["_id"] = "world", ["Player"] = new BsonDocument("PlayerId", playerId) },
                ["world_templates"] = new() { ["_id"] = "it", ["Player"] = new BsonDocument("PlayerId", Guid.Empty.ToString("N")) },
                ["events"] = new() { ["_id"] = Guid.NewGuid().ToString("N"), ["PlayerId"] = playerId },
                ["stars_memory"] = new() { ["_id"] = "memory", ["PlayerId"] = playerId },
                ["challenges"] = new() { ["_id"] = "challenge", ["PlayerId"] = playerId, ["Participants"] = new BsonArray { playerId } },
            };
            foreach (var (name, document) in legacyReferences)
            {
                await database.GetCollection<BsonDocument>(name).InsertOneAsync(document);
            }

            var first = CreatePost(ObjectId.GenerateNewId().ToString(), bsonDateTime);
            var second = CreatePost(Guid.NewGuid().ToString("N"), bsonDateTime);
            var current = CreatePost(PostId.New(CreatedAt).Value.ToString("N"), bsonDateTime);
            await posts.InsertManyAsync([first, second, current]);
            var publication = new BsonDocument
            {
                ["_id"] = "publication", ["Result"] = new BsonDocument("Post", first.DeepClone()), ["Text"] = "saved publication",
            };
            await publications.InsertOneAsync(publication);
            var orphan = CreatePost("orphan", bsonDateTime);
            await publications.InsertOneAsync(new BsonDocument { ["_id"] = "orphan-publication", ["Result"] = new BsonDocument("Post", orphan) });
            var completed = CreateTicker("completed", first["_id"].AsString, legacyTickerFormat);
            var pending = CreateTicker("pending", "pending-post", legacyTickerFormat);
            await tickers.InsertManyAsync([completed, pending]);
            var unrelated = CreateTicker("unrelated", "unchanged");
            unrelated["Function"] = "OtherJob";
            await tickers.InsertOneAsync(unrelated);

            await MigratePostIdsAsync(database);

            var migrated = await posts.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            await Assert.That(migrated.Count).IsEqualTo(3);
            await Assert.That(migrated.Select(post => post["_id"]).Distinct().Count()).IsEqualTo(3);
            var migratedFirst = migrated.Single(post => post["Text"] == first["Text"]);
            var migratedSecond = migrated.Single(post => post["Text"] == second["Text"]);
            await AssertVersion7Async(migratedFirst["_id"].AsBsonBinaryData.ToGuid().ToString("N"), CreatedAt);
            await AssertVersion7Async(migratedSecond["_id"].AsBsonBinaryData.ToGuid().ToString("N"), CreatedAt);
            await Assert.That(migratedFirst["_id"]).IsNotEqualTo(first["_id"]);
            await Assert.That(migratedSecond["_id"]).IsNotEqualTo(second["_id"]);
            await Assert.That(migrated.Single(post => post["Text"] == current["Text"])).IsEqualTo(new BsonDocument(current) { ["_id"] = new BsonBinaryData(Guid.Parse(current["_id"].AsString), GuidRepresentation.Standard) });
            var expected = first.DeepClone().AsBsonDocument;
            expected["_id"] = migratedFirst["_id"];
            await Assert.That(migratedFirst).IsEqualTo(expected);
            var savedPublication = await publications.Find(new BsonDocument("_id", "publication")).SingleAsync();
            await Assert.That(savedPublication["Result"]["Post"]).IsEqualTo(expected);
            await Assert.That(savedPublication["Text"]).IsEqualTo(publication["Text"]);
            var savedOrphan = await publications.Find(new BsonDocument("_id", "orphan-publication")).SingleAsync();
            await AssertVersion7Async(savedOrphan["Result"]["Post"]["_id"].AsBsonBinaryData.ToGuid().ToString("N"), CreatedAt);
            var savedTickers = await tickers.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            await Assert.That(ReadRequest(savedTickers.Single(ticker => ticker["_id"] == "completed")).PostId.Value.ToString("N")).IsEqualTo(migratedFirst["_id"].AsBsonBinaryData.ToGuid().ToString("N"));
            await AssertVersion7Async(ReadRequest(savedTickers.Single(ticker => ticker["_id"] == "pending")).PostId.Value.ToString("N"), CreatedAt.AddHours(1));
            await Assert.That(savedTickers.Single(ticker => ticker["_id"] == "unrelated")).IsEqualTo(unrelated);
            var migratedInvitation = await invitations.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
            await Assert.That(migratedInvitation["_id"].AsBsonBinaryData.SubType == BsonBinarySubType.UuidStandard).IsTrue();
            await Assert.That(migratedInvitation["InviteeId"].AsBsonBinaryData.ToGuid()).IsEqualTo(Guid.Parse(oldInvitation["InviteeId"].AsString));
            foreach (var (name, document) in legacyReferences)
            {
                var saved = await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
                var expectedReference = document.DeepClone().AsBsonDocument;
                var uuid = new BsonBinaryData(Guid.Parse(playerId), GuidRepresentation.Standard);
                switch (name)
                {
                    case "players":
                    {
                        expectedReference["_id"] = uuid;
                        break;
                    }

                    case "worlds":
                    {
                        expectedReference["Player"]["PlayerId"] = uuid;
                        break;
                    }

                    case "world_templates":
                    {
                        expectedReference["Player"]["PlayerId"] = new BsonBinaryData(Guid.Empty, GuidRepresentation.Standard);
                        break;
                    }

                    case "events":
                    {
                        expectedReference["_id"] = new BsonBinaryData(Guid.Parse(document["_id"].AsString), GuidRepresentation.Standard);
                        expectedReference["PlayerId"] = uuid;
                        break;
                    }

                    case "challenges":
                    {
                        expectedReference["PlayerId"] = uuid;
                        expectedReference["Participants"] = new BsonArray { uuid };
                        break;
                    }

                    default:
                    {
                        expectedReference["PlayerId"] = uuid;
                        break;
                    }
                }

                await Assert.That(saved).IsEqualTo(expectedReference);
            }

            await MigratePostIdsAsync(database);

            await Assert.That((await posts.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Select(document => document.ToJson())).IsEquivalentTo(migrated.Select(document => document.ToJson()));
            await Assert.That((await tickers.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Select(document => document.ToJson())).IsEquivalentTo(savedTickers.Select(document => document.ToJson()));

            await MigratePostIdsAsync(database, downgrade: true);

            await Assert.That((await posts.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Select(document => document.ToJson())).IsEquivalentTo(new[] { first, second, current }.Select(document => document.ToJson()));
            await Assert.That(await publications.Find(new BsonDocument("_id", "publication")).SingleAsync()).IsEqualTo(publication);
            await Assert.That((await publications.Find(new BsonDocument("_id", "orphan-publication")).SingleAsync())["Result"]["Post"]).IsEqualTo(orphan);
            await Assert.That(await invitations.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(oldInvitation);
            foreach (var (name, document) in legacyReferences)
            {
                await Assert.That(await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(document);
            }

            await Assert.That((await tickers.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Select(document => document.ToJson())).IsEquivalentTo(new[] { completed, pending, unrelated }.Select(document => document.ToJson()));
        }
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    public async Task Migrate_MissingTimestamp_RollsBackAllChangesAsync()
    {
        var database = mongo.MongoClient.GetDatabase($"post_ids_{Guid.NewGuid():N}");
        try
        {
            var posts = database.GetCollection<BsonDocument>("posts");
            var valid = CreatePost("valid-post", false);
            var invalid = CreatePost("missing-timestamp", false);
            invalid.Remove("CreatedAtUtc");
            await posts.InsertManyAsync([valid, invalid]);

            await Assert.That(() => MigratePostIdsAsync(database)).Throws<KeyNotFoundException>();

            await Assert.That((await posts.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Select(document => document.ToJson())).IsEquivalentTo(new[] { valid, invalid }.Select(document => document.ToJson()));
            await Assert.That(await database.GetCollection<BsonDocument>("migration_2_4_6_post_ids").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(0L);
        }
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    [Arguments("worlds")]
    [Arguments("world_templates")]
    public async Task NestWorldDocuments_RepeatedMigration_PreservesNestedDataAsync(string collectionName)
    {
        await using var scope = CreateDatabase();
        var database = scope.Database;
        using var session = await mongo.MongoClient.StartSessionAsync();
        var source = Common.TestGameData.CreateWorld();
        var expected = source with
        {
            Interface = source.Interface with { Title = "Свой мир", Difficulty = WorldDifficulty.Hard },
            Player = source.Player with
            {
                PlayerId = PlayerId.From(collectionName == "world_templates" ? "00000000000000000000000000000000" : "c7e5abd8b70c54a2d22beacd8a3e6377"),
                Avatar = "legacy-avatar",
                Drama = collectionName == "worlds" ? WorldDrama.Mid : WorldDrama.Hot,
                Subscribers = 9876,
                Wants = [new DailyWant("saved", "Своё желание", "опыт", true)],
                Achievements = [new AchievementProgress("saved", "⭐", "Достижение", true)],
                LastPostAtUtc = DateTimeOffset.UnixEpoch.AddHours(1),
                RestedPostsRemaining = 2,
                PeriodStartedAtUtc = DateTimeOffset.UnixEpoch,
            },
        };
        var original = expected.ToBsonDocument();
        var legacy = original.DeepClone().AsBsonDocument;
        var fields = legacy["Interface"].AsBsonDocument.Elements.Concat(legacy["Player"].AsBsonDocument.Elements).ToArray();
        foreach (var field in fields)
        {
            legacy[field.Name] = field.Value.DeepClone();
        }

        legacy.Remove("Interface");
        legacy.Remove("Player");
        legacy["Avatar"] = BsonNull.Value;
        legacy["Handle"] = "legacy-avatar";
        legacy["Drama"] = collectionName == "worlds" ? "unknown" : "hot";
        legacy["Presets"] = new BsonArray { "old-preset" };
        legacy["TotalPostsPublished"] = 20;
        legacy["PostsSinceViral"] = 15;
        legacy["ConsecutiveFlops"] = 3;
        var collection = database.GetCollection<BsonDocument>(collectionName);
        await collection.InsertOneAsync(legacy);
        await CreateIndexAsync(database, "worlds", "ix_worlds_player", new BsonDocument("PlayerId", 1), false);
        var migration = new NestWorldDocuments();
        await migration.UpAsync(database, session, CancellationToken.None);
        await migration.UpAsync(database, session, CancellationToken.None);
        var saved = await collection.Find(new BsonDocument("_id", original["_id"])).SingleAsync();
        await Assert.That(new BsonDocument(saved.Elements.OrderBy(field => field.Name, StringComparer.Ordinal)))
            .IsEqualTo(new BsonDocument(original.Elements.OrderBy(field => field.Name, StringComparer.Ordinal)));
        var hydrated = BsonSerializer.Deserialize<World>(saved);
        await Assert.That(hydrated.Player.PlayerId).IsEqualTo(expected.Player.PlayerId);
        await Assert.That(hydrated.Player.Drama).IsEqualTo(expected.Player.Drama);
        await Assert.That(hydrated.Player.Wants).IsEquivalentTo(expected.Player.Wants);
        await Assert.That(hydrated.Player.Achievements).IsEquivalentTo(expected.Player.Achievements);
        await Assert.That(hydrated.Interface).IsEqualTo(expected.Interface);
        using var indexes = await database.GetCollection<BsonDocument>("worlds").Indexes.ListAsync();
        await Assert.That((await indexes.ToListAsync()).Single(index => index["name"] == "ix_worlds_player")["key"])
            .IsEqualTo(new BsonDocument("Player.PlayerId", 1));
    }

    [Test]
    public async Task Migrate_EmptyDatabase_CreatesCurrentIndexesAsync()
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var migrator = CreateMigrator(mongo.MongoClient, database.DatabaseNamespace.DatabaseName);

        // Act
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);

        // Assert
        await AssertCurrentIndexesAsync(database);
        var playerCount = await database.GetCollection<BsonDocument>("players")
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
        await Assert.That(playerCount).IsEqualTo(0L);
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, true)]
    public async Task Migrate_ExistingDatabase_SafelyAdoptsCurrentStateAsync(
        bool existingIndexes,
        bool legacyIndex)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var player = new BsonDocument
            { { "_id", "test-player" }, { "TelegramUserId", 12345L }, { "World", "stream" } };
        await database.GetCollection<BsonDocument>("players")
            .InsertOneAsync(player, cancellationToken: CancellationToken.None);
        var challenge = new BsonDocument
        {
            { "_id", "test-challenge" }, { "PlayerId", "test-player" },
            { "Participants", new BsonArray { "test-player", "second-player" } }, { "Status", 1 },
        };
        await database.GetCollection<BsonDocument>("challenges")
            .InsertOneAsync(challenge, cancellationToken: CancellationToken.None);
        if (existingIndexes)
        {
            await SeedCurrentIndexesAsync(database);
        }

        if (legacyIndex)
        {
            await CreateIndexAsync(database, "challenges", "ux_challenges_active_player", new BsonDocument("PlayerId", 1), unique: true);
        }

        var migrator = CreateMigrator(mongo.MongoClient, database.DatabaseNamespace.DatabaseName);

        // Act
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);

        // Assert
        await AssertCurrentIndexesAsync(database);
        var savedPlayer = await database.GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("_id", "test-player")).SingleAsync(CancellationToken.None);
        var savedChallenge = await database.GetCollection<BsonDocument>("challenges")
            .Find(new BsonDocument("_id", "test-challenge")).SingleAsync(CancellationToken.None);
        await Assert.That(savedPlayer["_id"]).IsEqualTo(player["_id"]);
        await Assert.That(savedPlayer.Contains("World")).IsFalse();
        var worlds = await database.GetCollection<BsonDocument>("worlds").Find(new BsonDocument("Player.PlayerId", "test-player")).ToListAsync(CancellationToken.None);
        await Assert.That(worlds.Count).IsEqualTo(1);
        await Assert.That(worlds[0]["BalanceKey"].AsString).IsEqualTo("stream");
        await Assert.That(worlds[0]["Stars"].AsBsonArray.Count).IsEqualTo(3);
        await Assert.That(savedChallenge).IsEqualTo(challenge);
        var options = new MigrationOptions(database.DatabaseNamespace.DatabaseName);
        var historyCount = await database.GetCollection<BsonDocument>(options.MigrationsCollectionName)
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
        await Assert.That(historyCount).IsEqualTo(14L);
    }

    [Test]
    [Arguments("stage")]
    [Arguments("stream")]
    public async Task Migrate_TemplateWorld_ConsolidatesPlayerDataAsync(string kind)
    {
        await using var scope = CreateDatabase();
        var database = scope.Database;
        using var session = await mongo.MongoClient.StartSessionAsync();
        await new SeedWorldTemplates().UpAsync(database, session, CancellationToken.None);
        var template = await database.GetCollection<World>(MongoCollections.WorldTemplates)
            .Find(world => world.BalanceKey == kind).SingleAsync();
        var period = new BsonArray { DateTimeOffset.UnixEpoch.Ticks, 0 };
        var achievement = new AchievementProgress("saved-card", "🌊", "Свой момент", true).ToBsonDocument();
        achievement["WorldId"] = kind;
        var player = new BsonDocument
        {
            { "_id", "ca2b7afeed3ac05f1bda84ef172efad9" }, { "TelegramUserId", 12345L }, { "World", kind },
            { "Name", "Свой канал" }, { "Handle", "@legacy.channel" }, { "Role", "Своя роль" },
            { "Subscribers", 987L }, { "TotalPostsPublished", 15 }, { "PostsSinceViral", 4 },
            { "ConsecutiveFlops", 2 }, { "PostsPeriodStartedAtUtc", period.DeepClone() },
            { "Settings", new BsonDocument { { "Voice", "Свой голос" }, { "Drama", "soft" }, { "Quiet", "22:00–9:00" } } },
            { "Wants", new BsonArray { new DailyWant("saved-want", "Своё желание", "опыт", true).ToBsonDocument() } },
            { "Achievements", new BsonArray { achievement } },
            { "StarRelations", new BsonArray { new BsonDocument { { "StarId", template.Stars[0].Id.Value }, { "Value", 3 }, { "GainedInPeriod", 1 }, { "PeriodStartedAtUtc", period.DeepClone() } } } },
        };
        await database.GetCollection<BsonDocument>(MongoCollections.Players).InsertOneAsync(player);
        await database.GetCollection<BsonDocument>(MongoCollections.Events).InsertOneAsync(new BsonDocument
        {
            { "_id", "cc3fb9ee31be4b8b991163a6b06af36e" }, { "PlayerId", "ca2b7afeed3ac05f1bda84ef172efad9" }, { "Kind", "collab" },
            { "Badge", "событие" }, { "Title", "Коллаб" }, { "Text", "История" },
            { "Ideas", new BsonArray() }, { "Reward", "опыт" }, { "Note", "Ответ" },
            { "DayStartedAtUtc", period.DeepClone() }, { "Answer", BsonNull.Value },
        });

        if (kind == "stage")
        {
            await database.GetCollection<BsonDocument>(MongoCollections.Events).UpdateOneAsync(
                new BsonDocument("_id", "cc3fb9ee31be4b8b991163a6b06af36e"),
                Builders<BsonDocument>.Update.Set("WorldId", BsonNull.Value));
        }

        var migrator = CreateMigrator(mongo.MongoClient, database.DatabaseNamespace.DatabaseName);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);

        var world = await database.GetCollection<World>(MongoCollections.Worlds)
            .Find(new BsonDocument("Player.PlayerId", "ca2b7afeed3ac05f1bda84ef172efad9")).SingleAsync();
        await Assert.That(world.BalanceKey).IsEqualTo(kind);
        await Assert.That(world.Player.Name).IsEqualTo("Свой канал");
        await Assert.That(world.Player.Avatar).IsEqualTo("@legacy.channel");
        await Assert.That(world.Player.Role).IsEqualTo("Своя роль");
        await Assert.That(world.Player.Subscribers).IsEqualTo(987L);
        await Assert.That(world.Player.Voice).IsEqualTo("Свой голос");
        await Assert.That(world.Player.Drama).IsEqualTo(WorldDrama.Soft);
        await Assert.That(world.Player.Wants.Single().Done).IsTrue();
        await Assert.That(world.Player.Achievements.Single().IsOpen).IsTrue();
        await Assert.That(world.Stars[0].Relation).IsEqualTo(3);
        await Assert.That(world.Stars[0].RelationGainedInPeriod).IsEqualTo(1);
        await Assert.That(world.Stars[0].RelationPeriodStartedAtUtc).IsEqualTo(DateTimeOffset.UnixEpoch);
        await Assert.That(world.Roles).IsEquivalentTo(template.Roles);
        await Assert.That(world.Rules).IsEquivalentTo(template.Rules);
        await Assert.That(world.Interface.AvatarStyle).IsEqualTo(template.Interface.AvatarStyle);
        var savedEvent = await database.GetCollection<BsonDocument>(MongoCollections.Events)
            .Find(new BsonDocument("_id", "cc3fb9ee31be4b8b991163a6b06af36e")).SingleAsync();
        await Assert.That(BsonSerializer.Deserialize<DailyGameEvent>(savedEvent).WorldId).IsEqualTo(world.Id);
        var savedPlayer = await database.GetCollection<BsonDocument>(MongoCollections.Players)
            .Find(new BsonDocument("_id", "ca2b7afeed3ac05f1bda84ef172efad9")).SingleAsync();
        await Assert.That(savedPlayer.Contains("World")).IsFalse();
        await Assert.That(savedPlayer.Contains("Subscribers")).IsFalse();
        await Assert.That(savedPlayer.Contains("StarRelations")).IsFalse();
        await Assert.That(savedPlayer["Settings"].AsBsonDocument.Contains("Drama")).IsFalse();
        await Assert.That(savedPlayer["Settings"].AsBsonDocument.Contains("Voice")).IsFalse();
        await Assert.That(savedPlayer["Settings"]["Quiet"].AsString).IsEqualTo("22:00–9:00");
    }

    [Test]
    public async Task RepairLegacyDocuments_MissingValues_PreservesExistingDataAsync()
    {
        await using var scope = CreateDatabase();
        var database = scope.Database;
        using var session = await mongo.MongoClient.StartSessionAsync(cancellationToken: CancellationToken.None);
        (string Collection, string[] Fields)[] collections =
        [
            ("posts", ["Comments", "ThreadReplies"]),
            ("stars_memory", ["Memory", "Messages"]),
            ("events", ["Ideas"]),
            ("publications", ["Parts"]),
            ("worlds", ["Roles", "Voices", "Presets", "PostIdeas", "DemoPosts", "Rules", "Stars", "Wants", "Achievements"]),
            ("world_templates", ["Roles", "Voices", "Presets", "PostIdeas", "DemoPosts", "Rules", "Stars", "Wants", "Achievements"]),
        ];
        foreach (var (collectionName, fields) in collections)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var missing = new BsonDocument { { "_id", "missing" }, { "untouched", "original" } };
            var nullArrays = new BsonDocument { { "_id", "null" }, { "untouched", "original" } };
            nullArrays.AddRange(fields.Select(fieldName => new BsonElement(fieldName, BsonNull.Value)));
            var existing = new BsonDocument { { "_id", "existing" }, { "untouched", "original" } };
            existing.AddRange(fields.Select(fieldName => new BsonElement(fieldName, new BsonArray { "keep", BsonNull.Value })));

            await collection.InsertManyAsync([missing, nullArrays, existing], cancellationToken: CancellationToken.None);
        }

        var skills = new BsonDocument
        {
            { "missing", new BsonDocument { { "Level", 2 }, { "Progress", 0.5 } } },
            { "null", new BsonDocument { { "Level", 3 }, { "Progress", 0.5 }, { "Experience", BsonNull.Value } } },
            { "zero", new BsonDocument { { "Level", 2 }, { "Progress", 0.5 }, { "Experience", 0 } } },
            { "maximum", new BsonDocument { { "Level", 10 }, { "Progress", 1 }, { "Experience", 0 } } },
            { "existing", new BsonDocument { { "Level", 2 }, { "Progress", 0.5 }, { "Experience", 15 } } },
        };
        var players = database.GetCollection<BsonDocument>("players");
        await players.InsertOneAsync(new BsonDocument { { "_id", "skills-player" }, { "Skills", skills } }, cancellationToken: CancellationToken.None);
        var migration = new RepairLegacyDocuments();
        await migration.UpAsync(database, session, CancellationToken.None);
        await migration.UpAsync(database, session, CancellationToken.None);
        foreach (var (collectionName, fields) in collections)
        {
            await AssertDocumentArraysAsync(database, collectionName, fields);
        }

        var repaired = await players.Find(new BsonDocument("_id", "skills-player")).SingleAsync(CancellationToken.None);
        var expectedSkills = skills.DeepClone().AsBsonDocument;
        expectedSkills["missing"]["Experience"] = 14.0;
        expectedSkills["null"]["Experience"] = 28.0;
        expectedSkills["zero"]["Experience"] = 14.0;
        expectedSkills["maximum"]["Experience"] = 270.0;
        await Assert.That(repaired["Skills"]).IsEqualTo(expectedSkills);
        await Assert.That(new SkillProgress(2, 0.5, repaired["Skills"]["zero"]["Experience"].ToDouble())
            .AddExperience(6, [0, 8, 20, 36, 56, 80, 110, 150, 200, 270]).Experience).IsEqualTo(20);

        var post = new GamePost(
            PostId.From("d9d4a347db06b4483654268785e235fa"),
            PlayerId.From("b27d9a691c953713a6fa4df4378d0a9f").ToAuthorId(),
            WorldId.From("world"),
            "post",
            10,
            [],
            DateTimeOffset.UnixEpoch,
            0,
            0,
            1,
            false,
            false).ToBsonDocument();
        post.Remove("Comments");
        post["ThreadReplies"] = BsonNull.Value;
        var posts = database.GetCollection<BsonDocument>("posts");
        await posts.InsertOneAsync(post, cancellationToken: CancellationToken.None);
        await migration.UpAsync(database, session, CancellationToken.None);
        var repairedPost = await posts.Find(new BsonDocument("_id", post["_id"])).SingleAsync(CancellationToken.None);
        await Assert.That(repairedPost["ThreadReplies"].AsBsonArray.Count).IsEqualTo(0);
        repairedPost.Remove("ThreadReplies");
        var hydrated = BsonSerializer.Deserialize<GamePost>(repairedPost);
        await Assert.That(hydrated.Comments.Count).IsEqualTo(0);
        await Assert.That(hydrated.Version).IsEqualTo(0);
        await Assert.That(hydrated.FindRequest("unknown")).IsNull();
    }

    [Test]
    public async Task MoveReferralState_LegacyCredits_PreservesAttributionWithoutDoubleCountingAsync()
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var period = new DateTimeOffset(2026, 10, 5, 6, 0, 0, TimeSpan.Zero);
        var players = database.GetCollection<BsonDocument>("players");
        var inviter = CreateLegacyReferralPlayer("inviter", null, period, 12, 2, false);
        var first = CreateLegacyReferralPlayer("first", "inviter", period.AddHours(1), 13, 1, true);
        var second = CreateLegacyReferralPlayer("second", "inviter", period.AddHours(2), 3, 0, false);
        var third = CreateLegacyReferralPlayer("third", "inviter", period.AddHours(3), 0, 0, false);
        var child = CreateLegacyReferralPlayer("first-child", "first", period.AddHours(6), 0, 0, false);
        await players.InsertManyAsync([inviter, first, second, third, child]);
        await database.GetCollection<BsonDocument>("posts").InsertManyAsync([
            new BsonDocument { ["_id"] = "later", ["PlayerId"] = "first", ["CreatedAtUtc"] = ReferralTimestamp(period.AddHours(5)) },
            new BsonDocument { ["_id"] = "earlier", ["PlayerId"] = "first", ["CreatedAtUtc"] = ReferralTimestamp(period.AddHours(4)) }]);
        using var session = await mongo.MongoClient.StartSessionAsync();
        var migration = new MoveReferralStateToInvites();

        // Act
        await migration.UpAsync(database, session, CancellationToken.None);
        await migration.UpAsync(database, session, CancellationToken.None);

        // Assert
        var saved = await players.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var legacyFields = new[] { "InvitedBy", "InvitedAtUtc", "FirstPostInviteBonus", "FirstPostInviteBonusUsed", "InviteBonusesEarnedInPeriod" };
        foreach (var containsLegacyField in saved.SelectMany(player => legacyFields.Select(player.Contains)))
        {
            await Assert.That(containsLegacyField).IsFalse();
        }

        await Assert.That(saved.Single(item => item["_id"] == "inviter")["DailyBonus"].AsInt32).IsEqualTo(2);
        await Assert.That(saved.Single(item => item["_id"] == "first")["DailyBonus"].AsInt32).IsEqualTo(3);
        await Assert.That(saved.Single(item => item["_id"] == "second")["DailyBonus"].AsInt32).IsEqualTo(3);
        var invitations = database.GetCollection<LegacyInvitation>("invites");
        await Assert.That(await invitations.CountDocumentsAsync(FilterDefinition<LegacyInvitation>.Empty)).IsEqualTo(4L);
        var childInvite = await invitations.Find(item => item.InviteeId == "first-child").SingleAsync();
        await Assert.That(childInvite.InviterId).IsEqualTo("first");
        await Assert.That(childInvite.InviterBonusUsedAtUtc).IsEqualTo(period.AddHours(6));
        var invites = await invitations.Find(item => item.InviterId == "inviter")
            .SortBy(item => item.CreatedAtUtc).ToListAsync();
        await Assert.That(invites.Count).IsEqualTo(3);
        var expectedIds = new[] { "first", "second", "third" };
        for (var index = 0; index < invites.Count; index++)
        {
            var invite = invites[index];
            var expectedId = expectedIds[index];
            await Assert.That(invite.Id).IsEqualTo($"legacy_{expectedId}");
            await Assert.That(invite.InviteeId).IsEqualTo(expectedId);
            await Assert.That(invite.InviterId).IsEqualTo("inviter");
            await Assert.That(invite.CreatedAtUtc).IsEqualTo(period.AddHours(index + 1));
            await Assert.That(invite.InviterBonusUsed).IsTrue();
            await Assert.That(invite.InviterBonusUsedAtUtc).IsEqualTo(index < 2 ? period.AddHours(index + 1) : (DateTimeOffset?)null);
            await Assert.That(invite.InviteeBonusUsed).IsEqualTo(index == 0);
            await Assert.That(invite.InviteeBonusUsedAtUtc).IsEqualTo(index == 0 ? period.AddHours(4) : (DateTimeOffset?)null);
        }
    }

    [Test]
    [Arguments("attribution")]
    [Arguments("created")]
    public async Task MoveReferralState_IncompleteLegacyHistory_CompletesWithFallbacksAsync(string missing)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var period = new DateTimeOffset(2026, 10, 5, 6, 0, 0, TimeSpan.Zero);
        var inviter = CreateLegacyReferralPlayer("inviter", null, period, 12, 2, false);
        var first = CreateLegacyReferralPlayer("first", "inviter", period.AddHours(1), 0, 0, false);
        var second = CreateLegacyReferralPlayer("second", "inviter", period.AddHours(2), 0, 0, false);
        if (missing == "created")
        {
            first["InvitedAtUtc"] = BsonNull.Value;
        }

        var source = missing == "attribution" ? new[] { inviter, first } : new[] { inviter, first, second };
        var players = database.GetCollection<BsonDocument>("players");
        await players.InsertManyAsync(source);
        using var session = await mongo.MongoClient.StartSessionAsync();
        var migration = new MoveReferralStateToInvites();

        // Act
        await migration.UpAsync(database, session, CancellationToken.None);
        await migration.UpAsync(database, session, CancellationToken.None);

        // Assert
        var savedInviter = await players.Find(new BsonDocument("_id", "inviter")).SingleAsync();
        await Assert.That(savedInviter["DailyBonus"].AsInt32).IsEqualTo(2);
        foreach (var original in source)
        {
            var saved = await players.Find(new BsonDocument("_id", original["_id"])).SingleAsync();
            await Assert.That(saved.Contains("InvitedBy")).IsFalse();
            await Assert.That(saved.Contains("InviteBonusesEarnedInPeriod")).IsFalse();
        }

        var invite = await database.GetCollection<LegacyInvitation>("invites")
            .Find(item => item.InviteeId == "first").SingleAsync();
        await Assert.That(invite.CreatedAtUtc).IsEqualTo(missing == "created" ? period : period.AddHours(1));
        await Assert.That(invite.InviterBonusUsedAtUtc).IsEqualTo(missing == "created" ? period : (DateTimeOffset?)null);
    }

    [Test]
    public async Task MoveReferralState_UnknownFirstPostDate_PreservesHistoricalBonusUntilResetAsync()
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var period = new DateTimeOffset(2026, 10, 5, 6, 0, 0, TimeSpan.Zero);
        var players = database.GetCollection<BsonDocument>("players");
        await players.InsertOneAsync(CreateLegacyReferralPlayer("friend", "inviter", period.AddHours(1), 8, 0, true));
        using var session = await mongo.MongoClient.StartSessionAsync();
        var migration = new MoveReferralStateToInvites();

        // Act
        await migration.UpAsync(database, session, CancellationToken.None);
        await migration.UpAsync(database, session, CancellationToken.None);

        // Assert
        var saved = await players.Find(new BsonDocument("_id", "friend")).SingleAsync();
        await Assert.That(saved["DailyBonus"].AsInt32).IsEqualTo(8);
        await Assert.That(saved.Contains("FirstPostInviteBonusUsed")).IsFalse();
        var invitation = await database.GetCollection<LegacyInvitation>("invites")
            .Find(item => item.InviteeId == "friend").SingleAsync();
        await Assert.That(invitation.InviterBonusUsed).IsTrue();
        await Assert.That(invitation.InviterBonusUsedAtUtc).IsNull();
        await Assert.That(invitation.InviteeBonusUsed).IsTrue();
        await Assert.That(invitation.InviteeBonusUsedAtUtc).IsNull();
    }

    [Test]
    public async Task Up_LegacyThread_PreservesAuthorsParentsRequestsReactionsAsync()
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var post = LegacyPost();
        var root = LegacyComment("root", false);
        root["Reactions"] = new BsonArray
        {
            new BsonDocument { ["Me"] = true, ["Emoji"] = "❤️" },
            new BsonDocument { ["Me"] = false, ["Avatar"] = "mira", ["Emoji"] = "✨" },
        };
        var reply = LegacyComment("reply", true);
        reply["EnergySpent"] = 0;
        reply["SubscriberBonus"] = 9;
        reply["OriginCommentId"] = "root";
        var followup = LegacyComment("followup", false);
        post["Comments"] = new BsonArray { root, reply, followup };
        post["ThreadReplies"] = new BsonArray
        {
            new BsonDocument { ["CommentId"] = "reply", ["TargetCommentId"] = "root", ["ReplyId"] = "saved-key", ["Reaction"] = "👏" },
        };
        await SeedAsync(database, post);

        // Act
        await MigrateAsync(database);
        var saved = await ReadAsync(database, "posts", "post");
        var comments = saved["Comments"].AsBsonArray;

        // Assert
        await Assert.That(saved["AuthorId"]).IsEqualTo(Author(0, "owner"));
        await Assert.That(saved["WorldId"]).IsEqualTo(new BsonString("world"));
        await Assert.That(saved.Contains("PlayerId") || saved.Contains("ThreadReplies")).IsFalse();
        await Assert.That(comments[0]["AuthorId"]).IsEqualTo(Author(1, "mira"));
        await Assert.That(comments[0]["ParentCommentId"].IsBsonNull).IsTrue();
        await Assert.That(comments[0]["RequestId"].IsBsonNull).IsTrue();
        await Assert.That(comments[0]["Reactions"]).IsEqualTo(new BsonArray
        {
            new BsonDocument { ["AuthorId"] = Author(0, "owner"), ["Emoji"] = "❤️" },
            new BsonDocument { ["AuthorId"] = Author(1, "mira"), ["Emoji"] = "✨" },
        });
        await Assert.That(comments[1]["AuthorId"]).IsEqualTo(Author(0, "owner"));
        await Assert.That(comments[1]["ParentCommentId"]).IsEqualTo(new BsonString("root"));
        await Assert.That(comments[1]["RequestId"]).IsEqualTo(new BsonString("saved-key"));
        await Assert.That(comments[1]["Reactions"]).IsEqualTo(new BsonArray { new BsonDocument { ["AuthorId"] = Author(1, "mira"), ["Emoji"] = "👏" } });
        await Assert.That(comments[2]["ParentCommentId"].IsBsonNull).IsTrue();
        foreach (var comment in comments.Select(value => value.AsBsonDocument))
        {
            await Assert.That(comment.Names.Order(StringComparer.Ordinal)).IsEquivalentTo(new[] { "_id", "AuthorId", "Hidden", "ParentCommentId", "Reactions", "RequestId", "Text" });
        }

        await Assert.That((await ReadAsync(database, "players", "owner"))["EnergyUsed"]).IsEqualTo(new BsonInt32(4));
        await Assert.That((await ReadAsync(database, "worlds", "world"))["Player"]["Subscribers"]).IsEqualTo(new BsonInt32(1000));
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, false)]
    [Arguments(false, true)]
    public async Task Up_HistoricalUsage_PreservesCountersVersionsPublicationResultAsync(bool existingVersions, bool nullVersions)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var post = LegacyPost();
        if (nullVersions)
        {
            post["Version"] = BsonNull.Value;
            post["Comments"] = BsonNull.Value;
            post["ThreadReplies"] = BsonNull.Value;
        }

        await SeedAsync(database, post);
        if (nullVersions)
        {
            await database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", "owner"), Builders<BsonDocument>.Update.Set("Version", BsonNull.Value));
            await database.GetCollection<BsonDocument>("worlds").UpdateOneAsync(new BsonDocument("_id", "world"), Builders<BsonDocument>.Update.Set("Version", BsonNull.Value));
        }

        var snapshot = post.DeepClone().AsBsonDocument;
        if (!existingVersions)
        {
            snapshot.Remove("WorldId");
        }

        await database.GetCollection<BsonDocument>("publications").InsertOneAsync(new BsonDocument
        {
            ["_id"] = "publication",
            ["WorldId"] = "world",
            ["Result"] = new BsonDocument { ["Post"] = snapshot, ["Subscribers"] = 1234 },
        });
        if (existingVersions)
        {
            await database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", "owner"), Builders<BsonDocument>.Update.Set("Version", 8));
            await database.GetCollection<BsonDocument>("worlds").UpdateOneAsync(new BsonDocument("_id", "world"), Builders<BsonDocument>.Update.Set("Version", 9));
        }

        // Act
        await MigrateAsync(database);
        var saved = await ReadAsync(database, "posts", "post");
        var player = await ReadAsync(database, "players", "owner");
        var world = await ReadAsync(database, "worlds", "world");
        var publication = await ReadAsync(database, "publications", "publication");

        // Assert
        await Assert.That(saved["Comments"]).IsEqualTo(new BsonArray());
        await Assert.That(saved["Version"]).IsEqualTo(new BsonInt32(0));
        await Assert.That(saved["AuthorId"]).IsEqualTo(Author(0, "owner"));
        await Assert.That(player["EnergyUsed"]).IsEqualTo(new BsonInt32(4));
        await Assert.That(player.Contains("PostsUsed")).IsFalse();
        await Assert.That(player["Version"]).IsEqualTo(new BsonInt32(existingVersions ? 8 : 0));
        await Assert.That(world["Version"]).IsEqualTo(new BsonInt32(existingVersions ? 9 : 0));
        await Assert.That(world["Player"]["Subscribers"]).IsEqualTo(new BsonInt32(1000));
        await Assert.That(new BsonDocument(publication["Result"]["Post"].AsBsonDocument.Elements.OrderBy(field => field.Name, StringComparer.Ordinal)))
            .IsEqualTo(new BsonDocument(saved.Elements.OrderBy(field => field.Name, StringComparer.Ordinal)));
        await Assert.That(publication["Result"]["Subscribers"]).IsEqualTo(new BsonInt32(1234));
        await Assert.That(await ReadAsync(database, "posts", "post")).IsEqualTo(saved);
        await Assert.That(await ReadAsync(database, "players", "owner")).IsEqualTo(player);
        await Assert.That(await ReadAsync(database, "worlds", "world")).IsEqualTo(world);
        await Assert.That(await ReadAsync(database, "publications", "publication")).IsEqualTo(publication);
    }

    [Test]
    public async Task Up_EmptyDatabase_InsertsCompleteDomainTemplatesAsync()
    {
        await using var scope = CreateDatabase();
        var database = scope.Database;
        using var session = await mongo.MongoClient.StartSessionAsync();
        await new SeedWorldTemplates().UpAsync(database, session, CancellationToken.None);
        var templates = await database.GetCollection<World>(MongoCollections.WorldTemplates)
            .Find(FilterDefinition<World>.Empty).ToListAsync();
        await Assert.That(templates.Select(world => world.Id.Value)).IsEquivalentTo(["stage", "academy", "it", "stream", "own"]);
        await Assert.That(templates.All(world => world.Player.PlayerId.Value == Guid.Empty)).IsTrue();
        await Assert.That(templates.All(world => world.Stars.Count > 0 && world.Roles.Count > 0 && world.Rules.Count > 0)).IsTrue();
        await Assert.That(templates.All(world => world.Player.Wants.Count == 3 && world.Player.Achievements.Count == 12)).IsTrue();
        await Assert.That(templates.SelectMany(world => world.Stars).All(star => Enum.IsDefined(star.Trait))).IsTrue();
        await Assert.That(templates.All(world => !string.IsNullOrWhiteSpace(world.Interface.AvatarStyle))).IsTrue();
    }

    [Test]
    public async Task Up_RepeatedRun_PreservesExistingDocumentsAsync()
    {
        await using var scope = CreateDatabase();
        var database = scope.Database;
        using var session = await mongo.MongoClient.StartSessionAsync();
        var migration = new SeedWorldTemplates();
        await migration.UpAsync(database, session, CancellationToken.None);
        var templates = database.GetCollection<World>(MongoCollections.WorldTemplates);
        var stageId = WorldId.From("stage");
        var template = await templates.Find(world => world.Id == stageId).SingleAsync();
        var edited = template with { Interface = template.Interface with { Title = "Обновлённый шаблон" } };
        await templates.ReplaceOneAsync(world => world.Id == stageId, edited);
        var owned = template.Instantiate(PlayerId.From("7914ac6b4a791783fb72443b5461c5e8"), DateTimeOffset.UnixEpoch) with
        {
            Player = template.Player with { PlayerId = PlayerId.From("7914ac6b4a791783fb72443b5461c5e8"), Subscribers = 987 },
            Stars = [template.Stars[0] with { Name = "Своя звезда", Relation = 3 }],
        };
        await database.GetCollection<World>(MongoCollections.Worlds).InsertOneAsync(owned);

        await migration.UpAsync(database, session, CancellationToken.None);

        var savedTemplate = await templates.Find(world => world.Id == stageId).SingleAsync();
        var savedWorld = await database.GetCollection<World>(MongoCollections.Worlds).Find(world => world.Id == owned.Id).SingleAsync();
        await Assert.That(await templates.CountDocumentsAsync(FilterDefinition<World>.Empty)).IsEqualTo(5L);
        await Assert.That(savedTemplate.Interface.Title).IsEqualTo(edited.Interface.Title);
        await Assert.That(savedWorld.Player.Subscribers).IsEqualTo(987L);
        await Assert.That(savedWorld.Stars.Single().Name).IsEqualTo("Своя звезда");
        await Assert.That(savedWorld.Stars.Single().Relation).IsEqualTo(3);
    }

    [Test]
    [Arguments(false, -1, 4, 15)]
    [Arguments(false, 0, 4, 15)]
    [Arguments(false, 1, 4, 24)]
    [Arguments(true, 1, 4, 37)]
    [Arguments(false, 1, 99, 0)]
    public async Task StoreRemainingEnergy_CustomBalance_PreservesLegacyAccountingAsync(bool plus, int expirySeconds, int spent, int expected)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        using var session = await mongo.MongoClient.StartSessionAsync();
        var period = new DateTimeOffset(2020, 1, 1, 6, 0, 0, TimeSpan.Zero);
        var legacy = new BsonDocument
        {
            ["_id"] = "energy-player",
            ["Settings"] = new BsonDocument("Plus", plus),
            ["PostsPeriodStartedAtUtc"] = ReferralTimestamp(period),
            ["ChallengeBonusUntilUtc"] = ReferralTimestamp(period.AddSeconds(expirySeconds)),
            ["ChallengeDailyBonus"] = 9,
            ["DailyBonus"] = 2,
            ["EnergyUsed"] = spent,
            ["Version"] = 7,
        };
        await database.GetCollection<BsonDocument>("players").InsertOneAsync(legacy);
        var migration = new StoreRemainingEnergy(Options.Create(CreateEnergyBalance() with { FreePostsPerDay = 17, PlusPostsPerDay = 30 }));

        // Act
        await migration.UpAsync(database, session, CancellationToken.None);
        await migration.UpAsync(database, session, CancellationToken.None);

        // Assert
        var saved = await ReadAsync(database, "players", "energy-player");
        await Assert.That(saved["Energy"].AsInt32).IsEqualTo(expected);
        await Assert.That(saved["EnergyUsed"].AsInt32).IsEqualTo(spent);
        await Assert.That(saved["PostsPeriodStartedAtUtc"]).IsEqualTo(legacy["PostsPeriodStartedAtUtc"]);
        await Assert.That(saved["ChallengeBonusUntilUtc"]).IsEqualTo(legacy["ChallengeBonusUntilUtc"]);
        await Assert.That(saved["Version"].AsInt32).IsEqualTo(7);
        await Assert.That(saved.Contains("DailyBonus")).IsFalse();
        await Assert.That(saved.Contains("ChallengeDailyBonus")).IsFalse();
    }

    [Test]
    [Arguments(0)]
    [Arguments(6)]
    public async Task StoreRemainingEnergy_ExistingEnergy_DoesNotReplayLegacyBonusesAsync(int energy)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        using var session = await mongo.MongoClient.StartSessionAsync();
        var legacy = new BsonDocument
        {
            ["_id"] = "stored-energy",
            ["Energy"] = energy,
            ["EnergyUsed"] = 4,
            ["DailyBonus"] = 7,
            ["ChallengeDailyBonus"] = 3,
        };
        await database.GetCollection<BsonDocument>("players").InsertOneAsync(legacy);
        var migration = new StoreRemainingEnergy(Options.Create(CreateEnergyBalance()));

        // Act
        await migration.UpAsync(database, session, CancellationToken.None);
        await migration.UpAsync(database, session, CancellationToken.None);

        // Assert
        var saved = await ReadAsync(database, "players", "stored-energy");
        await Assert.That(saved["Energy"].AsInt32).IsEqualTo(energy);
        await Assert.That(saved["EnergyUsed"].AsInt32).IsEqualTo(4);
        await Assert.That(saved.Contains("DailyBonus")).IsFalse();
        await Assert.That(saved.Contains("ChallengeDailyBonus")).IsFalse();
    }

    [Test]
    [Arguments(0)]
    [Arguments(3)]
    public async Task AddBonusAmountToInvite_RepeatedMigration_SetsConfiguredAmountForAllInvitesAsync(int inviteCount)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        using var session = await mongo.MongoClient.StartSessionAsync();
        var invites = database.GetCollection<BsonDocument>(MongoCollections.Invites);
        var originals = Enumerable.Range(0, inviteCount).Select(index => new BsonDocument
        {
            ["_id"] = $"invite-{index}",
            ["InviterId"] = "inviter",
            ["InviteeId"] = $"invitee-{index}",
            ["InviterBonusUsed"] = index > 0,
            ["InviteeBonusUsed"] = index > 1,
        }).ToArray();
        if (originals.Length > 0)
        {
            originals[^1]["BonusAmount"] = 99;
            await invites.InsertManyAsync(originals, cancellationToken: CancellationToken.None);
        }

        var migration = new AddBonusAmountToInvite(Options.Create(CreateEnergyBalance() with { InviteBonusBoth = 17 }));

        // Act
        await migration.UpAsync(database, session, CancellationToken.None);
        await migration.UpAsync(database, session, CancellationToken.None);

        // Assert
        var saved = await invites.Find(FilterDefinition<BsonDocument>.Empty)
            .SortBy(document => document["_id"]).ToListAsync(CancellationToken.None);
        await Assert.That(saved.Count).IsEqualTo(inviteCount);
        for (var index = 0; index < originals.Length; index++)
        {
            var expected = originals[index].DeepClone().AsBsonDocument;
            expected["BonusAmount"] = 17;
            await Assert.That(saved[index]).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task ReleaseCatalog_PreservesExistingDataAsync()
    {
        await using var scope = CreateDatabase();
        var database = scope.Database;
        using var session = await mongo.MongoClient.StartSessionAsync();
        await new SeedWorldTemplates().UpAsync(database, session, CancellationToken.None);
        var templates = database.GetCollection<BsonDocument>("world_templates");
        var it = await templates.Find(new BsonDocument("_id", "it")).SingleAsync();
        var existing = Common.TestGameData.CreateWorld().ToBsonDocument();
        await database.GetCollection<BsonDocument>("worlds").InsertOneAsync(existing);
        session.StartTransaction();
        await new ReleaseWorldCatalog().UpAsync(database, session, CancellationToken.None);
        await session.CommitTransactionAsync();
        var catalog = await templates.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        await Assert.That(catalog.Select(item => item["_id"].AsString)).IsEquivalentTo(["it", "evening", "sorcerers", "houses"]);
        await Assert.That(catalog.Single(item => item["_id"] == "it")).IsEqualTo(it);
        await Assert.That(await database.GetCollection<BsonDocument>("worlds").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(existing);
    }

    [Test]
    public async Task Migrate_CurrentCatalog_EngineDoesNotApplyTwiceAsync()
    {
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var migrator = CreateMigrator(mongo.MongoClient, database.DatabaseNamespace.DatabaseName);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        var templates = database.GetCollection<BsonDocument>("world_templates");
        var before = await templates.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        var after = await templates.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        await Assert.That(after.Count).IsEqualTo(4);
        await Assert.That(after.OrderBy(item => item["_id"].AsString, StringComparer.Ordinal).Select(item => item.ToJson()))
            .IsEquivalentTo(before.OrderBy(item => item["_id"].AsString, StringComparer.Ordinal).Select(item => item.ToJson()));
    }

    [Test]
    public async Task ReleaseCatalog_UsesOneWorldOrReceivedInviterBonusAsync()
    {
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var players = database.GetCollection<BsonDocument>("players");
        await players.InsertManyAsync(new[] { "threshold", "referral", "split", "locked" }.Select(id => new BsonDocument("_id", id)));
        await database.GetCollection<BsonDocument>("worlds").InsertManyAsync(new[]
        {
            new BsonDocument { ["_id"] = "first", ["Player"] = new BsonDocument { ["PlayerId"] = "threshold", ["Subscribers"] = 500 } },
            new BsonDocument { ["_id"] = "second", ["Player"] = new BsonDocument { ["PlayerId"] = "split", ["Subscribers"] = 250 } },
            new BsonDocument { ["_id"] = "third", ["Player"] = new BsonDocument { ["PlayerId"] = "split", ["Subscribers"] = 250 } },
        });
        await database.GetCollection<BsonDocument>("invites").InsertManyAsync(new[]
        {
            new BsonDocument { ["InviterId"] = "referral", ["InviterBonusUsed"] = true },
            new BsonDocument { ["InviterId"] = "locked", ["InviterBonusUsed"] = false },
        });
        using var session = await mongo.MongoClient.StartSessionAsync();
        await new ReleaseWorldCatalog().UpAsync(database, session, CancellationToken.None);
        var result = await players.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        await Assert.That(result.Where(player => player["HitOwnWorldMilestone"].AsBoolean).Select(player => player["_id"].AsString)).IsEquivalentTo(["threshold", "referral"]);
    }

    [Test]
    public async Task ReleaseCatalog_UpgradesExistingProductionDatabaseAsync()
    {
        await using var scope = CreateDatabase();
        var database = scope.Database;
        await CreateMigrator(mongo.MongoClient, database.DatabaseNamespace.DatabaseName, false)
            .MigrateAsync(cancellationToken: CancellationToken.None);
        var templates = database.GetCollection<BsonDocument>("world_templates");
        var it = await templates.Find(new BsonDocument("_id", "it")).SingleAsync();
        var players = database.GetCollection<BsonDocument>("players");
        await players.InsertManyAsync([
            new BsonDocument { { "_id", "already" }, { "TelegramUserId", 10001L }, { "HitOwnWorldMilestone", true } },
            new BsonDocument { { "_id", "threshold" }, { "TelegramUserId", 10002L } },
            new BsonDocument { { "_id", "locked" }, { "TelegramUserId", 10003L } },
        ]);
        var existing = Common.TestGameData.CreateWorld().ToBsonDocument();
        existing["Player"]["PlayerId"] = "threshold";
        existing["Player"]["Subscribers"] = 500;
        var worlds = database.GetCollection<BsonDocument>("worlds");
        await worlds.InsertOneAsync(existing);
        var migrator = CreateMigrator(mongo.MongoClient, database.DatabaseNamespace.DatabaseName);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        var catalog = await templates.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        await Assert.That(catalog.Select(item => item["_id"].AsString)).IsEquivalentTo(["it", "evening", "sorcerers", "houses"]);
        await Assert.That(catalog.Single(item => item["_id"] == "it")).IsEqualTo(it);
        await Assert.That(await worlds.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(existing);
        var result = await players.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        await Assert.That(result.Where(player => player["HitOwnWorldMilestone"].AsBoolean).Select(player => player["_id"].AsString))
            .IsEquivalentTo(["already", "threshold"]);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Migrate_LegacyThreads_RemovesOnlyUnusedCollectionAsync(bool existingThreads)
    {
        // Arrange: model a database where migrations through 2.4.3 have already run.
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var databaseName = database.DatabaseNamespace.DatabaseName;
        await CreateMigrator(mongo.MongoClient, databaseName, includeThreadCleanup: false)
            .MigrateAsync(cancellationToken: CancellationToken.None);
        if (existingThreads)
        {
            await database.GetCollection<BsonDocument>("threads").InsertOneAsync(
                new BsonDocument { { "_id", "legacy-thread" }, { "PlayerId", "owner" }, { "Hook", "legacy hook" } });
            await CreateIndexAsync(database, "threads", "ix_threads_player", new BsonDocument("PlayerId", 1), false);
        }

        var post = new BsonDocument
        {
            { "_id", "saved-post" },
            {
                "Comments", new BsonArray
                {
                    new BsonDocument { { "_id", "star-comment" }, { "Text", "saved comment" } },
                    new BsonDocument { { "_id", "player-reply" }, { "ParentId", "star-comment" }, { "Text", "saved reply" } },
                }
            },
        };
        await database.GetCollection<BsonDocument>("posts").InsertOneAsync(post);
        var options = new RekiMigrationOptions(Options.Create(new MongoOptions { DatabaseName = databaseName }));
        var history = database.GetCollection<BsonDocument>(options.MigrationsCollectionName);
        var originalHistory = await history.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        using var namesCursor = await database.ListCollectionNamesAsync();
        var otherNames = (await namesCursor.ToListAsync())
            .Where(name => name != "threads" && name != options.MigrationsCollectionName && name != options.MigrationsLockCollectionName).ToArray();
        var snapshots = new Dictionary<string, List<BsonDocument>>(StringComparer.Ordinal);
        foreach (var name in otherNames)
        {
            snapshots[name] = await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        }

        // Act: run the normal migrator twice, then retry the drop itself after removal.
        var migrator = CreateMigrator(mongo.MongoClient, databaseName);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        using var session = await mongo.MongoClient.StartSessionAsync();
        await new RemoveUnusedThreads().UpAsync(database, session, CancellationToken.None);

        // Assert
        using var currentNamesCursor = await database.ListCollectionNamesAsync();
        var currentNames = await currentNamesCursor.ToListAsync();
        await Assert.That(currentNames).DoesNotContain("threads");
        foreach (var (name, expected) in snapshots)
        {
            var saved = await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            await Assert.That(saved.Select(document => document.ToJson())).IsEquivalentTo(expected.Select(document => document.ToJson()));
        }

        var currentHistory = await history.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        await Assert.That(currentHistory.Count).IsEqualTo(originalHistory.Count + 1);
        foreach (var entry in originalHistory)
        {
            await Assert.That(currentHistory.Select(document => document.ToJson())).Contains(entry.ToJson());
        }

        await AssertCurrentIndexesAsync(database);
    }

    private static BsonDocument CreatePost(string id, bool bsonDateTime)
    {
        var document = new GamePost(PostId.New(), PlayerId.From(Guid.Parse("a96fb5ddf0564809a1d2bb5ed78a210f")).ToAuthorId(), WorldId.From("world"), id, 123, [], CreatedAt, 7, 0.8, 1.2, false, false, 5).ToBsonDocument();
        document["_id"] = id;
        document["Extra"] = new BsonDocument("Retained", true);
        if (bsonDateTime)
        {
            document["CreatedAtUtc"] = new BsonDateTime(CreatedAt.UtcDateTime);
        }

        return document;
    }

    private static BsonDocument CreateTicker(string id, string postId, bool legacyFormat = false) => new()
    {
        ["_id"] = id,
        ["Function"] = nameof(GenerateStarFeedPostJob),
        ["Request"] = new BsonBinaryData(TickerHelper.CreateTickerRequest(System.Text.Encoding.UTF8.GetBytes(new System.Text.Json.Nodes.JsonObject { [legacyFormat ? "PlayerId" : "playerId"] = "a96fb5ddf0564809a1d2bb5ed78a210f", [legacyFormat ? "WorldId" : "worldId"] = "world", [legacyFormat ? "PostId" : "postId"] = postId }.ToJsonString()))),
        ["CreatedAt"] = new BsonDateTime(CreatedAt.AddHours(1).UtcDateTime),
        ["Retries"] = 3,
    };

    private static StarFeedRequest ReadRequest(BsonDocument ticker) =>
        TickerHelper.ReadTickerRequest(ticker["Request"].AsBsonBinaryData.Bytes, ApiJsonSerializerContext.Default.StarFeedRequest);

    private static async Task AssertVersion7Async(string id, DateTimeOffset createdAt)
    {
        await Assert.That(Guid.ParseExact(id, "N").Version).IsEqualTo(7);
        await Assert.That(long.Parse(id[..12], NumberStyles.HexNumber, CultureInfo.InvariantCulture)).IsEqualTo(createdAt.ToUnixTimeMilliseconds());
    }

    private static GameBalance CreateEnergyBalance() => new(10, 20, new TimeOnly(9, 0), 5, 3, 5, 3, 3, new GrowthBalance(1, 1, [], 0, 1, 1, 2, 3, 2, 1000), new ViralBalance(0, 0, 1), new FlopBalance(1), new SkillBalance([], 1, 1, 10, 2, 3, 1, 1), new RelationBalance(0, 10, 3, 5, 3), new ChallengeBalance(3, 1), new ThreadBalance(3, 9), new Dictionary<string, WorldBalance>());

    private static BsonDocument CreateLegacyReferralPlayer(string id, string? inviter, DateTimeOffset createdAt, int dailyBonus, int count, bool used) => new()
    {
        ["_id"] = id,
        ["DailyBonus"] = dailyBonus,
        ["PostsPeriodStartedAtUtc"] = ReferralTimestamp(new DateTimeOffset(2026, 10, 5, 6, 0, 0, TimeSpan.Zero)),
        ["InvitedBy"] = inviter is null ? BsonNull.Value : inviter,
        ["InvitedAtUtc"] = inviter is null ? BsonNull.Value : ReferralTimestamp(createdAt),
        ["FirstPostInviteBonus"] = inviter is null ? 0 : 5,
        ["FirstPostInviteBonusUsed"] = used,
        ["InviteBonusesEarnedInPeriod"] = count,
    };

    private static BsonValue ReferralTimestamp(DateTimeOffset value) =>
        Invitation.Create(PlayerId.New(), PlayerId.New(), value, 0)
            .ToBsonDocument()["CreatedAtUtc"];

    private static async Task AssertDocumentArraysAsync(IMongoDatabase database, string collectionName, IReadOnlyList<string> fields)
    {
        var documents = await database.GetCollection<BsonDocument>(collectionName)
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(CancellationToken.None);
        await Assert.That(documents.Count).IsEqualTo(3);
        foreach (var document in documents)
        {
            var array = document["_id"] == "existing"
                ? new BsonArray { "keep", BsonNull.Value } : new BsonArray();
            var expected = new BsonDocument { { "_id", document["_id"] }, { "untouched", "original" } };
            expected.AddRange(fields.Select(fieldName => new BsonElement(fieldName, array.DeepClone())));
            await Assert.That(document).IsEqualTo(expected);
        }
    }

    private static IMigrator CreateMigrator(IMongoClient client, string databaseName, bool includeRelease = true, bool includeThreadCleanup = true)
    {
        var options = new RekiMigrationOptions(Options.Create(new MongoOptions { DatabaseName = databaseName }));
        var migrations = new List<RekiMigrationBase>
        {
            new InitialGameIndexes(), new RemoveLegacyChallengeIndex(), new SeedWorldTemplates(), new ConsolidateWorldSchema(),
            new RepairLegacyDocuments(), new NestWorldDocuments(), new MoveReferralStateToInvites(), new UnifyPostContent(),
            new CreateGameDayIndexes(), new StoreRemainingEnergy(Options.Create(CreateEnergyBalance())),
            new AddBonusAmountToInvite(Options.Create(CreateEnergyBalance())),
        };
        if (includeRelease)
        {
            migrations.Add(new ReleaseWorldCatalog());
            migrations.Add(new RemoveStarAppearanceFields());
        }

        if (includeRelease && includeThreadCleanup)
        {
            migrations.Add(new RemoveUnusedThreads());
        }

        return MigratorBuilder.FromMongoClient(client, options).LoadMigrations(migrations).Build();
    }

    private static IEnumerable<(string Collection, string Name, BsonDocument Keys, bool Unique)> ExpectedIndexes()
    {
        yield return ("players", "ux_players_telegram_user", new BsonDocument("TelegramUserId", 1), true);
        yield return ("publications", "ix_publications_expiry", new BsonDocument("ExpiresAtUtc", 1), false);
        yield return ("stars_memory", "ux_stars_memory_world_star",
            new BsonDocument { { "PlayerId", 1 }, { "WorldId", 1 }, { "StarId", 1 } }, true);
        yield return ("events", "ix_events_player_day",
            new BsonDocument { { "PlayerId", 1 }, { "DayStartedAtUtc", -1 } }, false);
        yield return ("challenges", "ux_challenges_active_participants", new BsonDocument("Participants", 1), true);
        yield return ("players", "ix_players_day_period", new BsonDocument("PostsPeriodStartedAtUtc", 1), false);
        yield return ("worlds", "ix_worlds_day_period", new BsonDocument("Player.PeriodStartedAtUtc", 1), false);
        yield return ("worlds", "ix_worlds_player", new BsonDocument("Player.PlayerId", 1), false);
        yield return ("posts", "ix_posts_world_created", new BsonDocument { { "WorldId", 1 }, { "CreatedAtUtc", -1 } }, false);
        yield return ("events", "ix_events_world_day", new BsonDocument { { "PlayerId", 1 }, { "WorldId", 1 }, { "DayStartedAtUtc", 1 } }, false);
        yield return ("invites", "ix_invites_player", new BsonDocument("PlayerId", 1), false);
        yield return ("invites", "ux_invites_invitee", new BsonDocument("InviteeId", 1), true);
        yield return ("invites", "ix_invites_inviter", new BsonDocument("InviterId", 1), false);
    }

    private static async Task SeedCurrentIndexesAsync(IMongoDatabase database)
    {
        await CreateIndexAsync(database, "posts", "ix_posts_player_created", new BsonDocument { { "PlayerId", 1 }, { "CreatedAtUtc", -1 } }, false);
        foreach (var (collection, name, keys, unique) in ExpectedIndexes())
        {
            var legacyKeys = name == "ix_posts_world_created" ? new BsonDocument { { "PlayerId", 1 }, { "WorldId", 1 }, { "CreatedAtUtc", 1 } } : keys;
            await CreateIndexAsync(database, collection, name, name == "ix_worlds_player" ? new BsonDocument("PlayerId", 1) : legacyKeys, unique);
        }
    }

    private static Task<string> CreateIndexAsync(
        IMongoDatabase database,
        string collection,
        string name,
        BsonDocument keys,
        bool unique)
    {
        var options = new CreateIndexOptions<BsonDocument> { Name = name, Unique = unique };
        if (name == "ix_publications_expiry")
        {
            options.ExpireAfter = TimeSpan.Zero;
        }

        if (name == "ux_challenges_active_participants")
        {
            options.PartialFilterExpression = new BsonDocument("Status", 1);
        }

        if (name == "ux_invites_invitee")
        {
            options.PartialFilterExpression = new BsonDocument("InviteeId", new BsonDocument("$type", "string"));
        }

        return database.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(keys, options), cancellationToken: CancellationToken.None);
    }

    private static async Task AssertCurrentIndexesAsync(IMongoDatabase database)
    {
        foreach (var (collection, name, keys, unique) in ExpectedIndexes())
        {
            using var cursor = await database.GetCollection<BsonDocument>(collection).Indexes
                .ListAsync(CancellationToken.None);
            var indexes = await cursor.ToListAsync(CancellationToken.None);
            var index = indexes.Single(item => item["name"] == name);
            await Assert.That(index["key"]).IsEqualTo(keys);
            await Assert.That(indexes.Any(item => item["name"] == "ix_posts_player_created")).IsFalse();
            await Assert.That(index.GetValue("unique", false).AsBoolean).IsEqualTo(unique);
            await Assert.That(indexes.Any(item => item["name"] == "ux_challenges_active_player")).IsFalse();
            if (name == "ix_publications_expiry")
            {
                await Assert.That(index["expireAfterSeconds"].ToInt64()).IsEqualTo(0L);
            }

            if (name == "ux_challenges_active_participants")
            {
                await Assert.That(index["partialFilterExpression"]).IsEqualTo(new BsonDocument("Status", 1));
            }
        }
    }

    private static BsonString Author(int kind, string id) => new($"{(kind == 0 ? "Player" : "Star")}_{id}");

    private static BsonDocument LegacyPost() => new() { ["_id"] = "post", ["WorldId"] = "world", ["PlayerId"] = "owner", ["Text"] = "Пост" };

    private static BsonDocument LegacyComment(string id, bool me) => new()
    {
        ["_id"] = id,
        ["Text"] = "Комментарий",
        ["Me"] = me,
        ["Author"] = me ? "Канал" : "Мира",
        ["Avatar"] = me ? "player-avatar" : "mira",
        ["Star"] = !me,
    };

    private static async Task SeedAsync(IMongoDatabase database, BsonDocument post)
    {
        await database.GetCollection<BsonDocument>("worlds").InsertOneAsync(new BsonDocument
        {
            ["_id"] = "world",
            ["Player"] = new BsonDocument { ["PlayerId"] = "owner", ["Subscribers"] = 1000 },
            ["Stars"] = new BsonArray { new BsonDocument { ["_id"] = "mira", ["Name"] = "Мира" } },
        });
        await database.GetCollection<BsonDocument>("players").InsertOneAsync(new BsonDocument { ["_id"] = "owner", ["PostsUsed"] = 4 });
        await database.GetCollection<BsonDocument>("posts").InsertOneAsync(post);
    }

    private static Task<BsonDocument> ReadAsync(IMongoDatabase database, string collection, string id) =>
        database.GetCollection<BsonDocument>(collection).Find(new BsonDocument("_id", id)).SingleAsync();

    private async Task MigratePostIdsAsync(IMongoDatabase database, bool downgrade = false)
    {
        using var session = await mongo.MongoClient.StartSessionAsync();
        var migration = new UseVersion7PostIds();
        session.StartTransaction();
        try
        {
            if (downgrade)
            {
                await migration.DownAsync(database, session, CancellationToken.None);
            }
            else
            {
                await migration.UpAsync(database, session, CancellationToken.None);
            }

            await session.CommitTransactionAsync();
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    private async Task MigrateAsync(IMongoDatabase database)
    {
        using var session = await mongo.MongoClient.StartSessionAsync();
        session.StartTransaction();
        try
        {
            await new UnifyPostContent().UpAsync(database, session, CancellationToken.None);
            await session.CommitTransactionAsync();
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    private MigrationDatabase CreateDatabase() => new(mongo.MongoClient);

    private sealed record LegacyInvitation(string Id, string InviterId, string InviteeId, DateTimeOffset CreatedAtUtc, bool InviterBonusUsed, bool InviteeBonusUsed, int BonusAmount, DateTimeOffset? InviterBonusUsedAtUtc, DateTimeOffset? InviteeBonusUsedAtUtc);

    private sealed class MigrationDatabase(IMongoClient client) : IAsyncDisposable
    {
        public IMongoDatabase Database { get; } = client.GetDatabase($"migration_{Guid.NewGuid():N}");

        public ValueTask DisposeAsync() => new(client.DropDatabaseAsync(Database.DatabaseNamespace.DatabaseName, CancellationToken.None));
    }
}
