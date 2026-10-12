using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Persistence;
using Reki.Infrastructure.Persistence.Migrations;

namespace Reki.Tests.Persistence;

[ClassDataSource<TestSessionFixture>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Migrations", Order = 0)]
public sealed class GameIndexMigrationTests(TestSessionFixture mongo)
{
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
                PlayerId = PlayerId.From(collectionName == "world_templates" ? "00000000000000000000000000000000" : "world-owner"),
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
        await Assert.That(historyCount).IsEqualTo(8L);
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
            { "_id", "legacy-player" }, { "TelegramUserId", 12345L }, { "World", kind },
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
            { "_id", "legacy-event" }, { "PlayerId", "legacy-player" }, { "Kind", "collab" },
            { "Badge", "событие" }, { "Title", "Коллаб" }, { "Text", "История" },
            { "Ideas", new BsonArray() }, { "Reward", "опыт" }, { "Note", "Ответ" },
            { "DayStartedAtUtc", period.DeepClone() }, { "Answer", BsonNull.Value },
        });

        if (kind == "stage")
        {
            await database.GetCollection<BsonDocument>(MongoCollections.Events).UpdateOneAsync(
                new BsonDocument("_id", "legacy-event"),
                Builders<BsonDocument>.Update.Set("WorldId", BsonNull.Value));
        }

        var migrator = CreateMigrator(mongo.MongoClient, database.DatabaseNamespace.DatabaseName);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        await migrator.MigrateAsync(cancellationToken: CancellationToken.None);

        var world = await database.GetCollection<World>(MongoCollections.Worlds)
            .Find(new BsonDocument("Player.PlayerId", "legacy-player")).SingleAsync();
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
            .Find(new BsonDocument("_id", "legacy-event")).SingleAsync();
        await Assert.That(BsonSerializer.Deserialize<Domain.Events.DailyGameEvent>(savedEvent).WorldId).IsEqualTo(world.Id);
        var savedPlayer = await database.GetCollection<BsonDocument>(MongoCollections.Players)
            .Find(new BsonDocument("_id", "legacy-player")).SingleAsync();
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
            PostId.From("hydrated-post"),
            PlayerId.From("player").ToAuthorId(),
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
        var repairedPost = await posts.Find(new BsonDocument("_id", "hydrated-post")).SingleAsync(CancellationToken.None);
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
        var invitations = database.GetCollection<Invitation>("invites");
        await Assert.That(await invitations.CountDocumentsAsync(FilterDefinition<Invitation>.Empty)).IsEqualTo(4L);
        var childInvite = await invitations.Find(item => item.InviteeId == PlayerId.From("first-child")).SingleAsync();
        await Assert.That(childInvite.InviterId.Value).IsEqualTo("first");
        await Assert.That(childInvite.InviterBonusUsedAtUtc).IsEqualTo(period.AddHours(6));
        var invites = await invitations.Find(item => item.InviterId == PlayerId.From("inviter"))
            .SortBy(item => item.CreatedAtUtc).ToListAsync();
        await Assert.That(invites.Count).IsEqualTo(3);
        var expectedIds = new[] { "first", "second", "third" };
        for (var index = 0; index < invites.Count; index++)
        {
            var invite = invites[index];
            var expectedId = expectedIds[index];
            await Assert.That(invite.Id.Value).IsEqualTo($"legacy_{expectedId}");
            await Assert.That(invite.InviteeId.Value).IsEqualTo(expectedId);
            await Assert.That(invite.InviterId.Value).IsEqualTo("inviter");
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

        var invite = await database.GetCollection<Invitation>("invites")
            .Find(item => item.InviteeId == PlayerId.From("first")).SingleAsync();
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
        var invitation = await database.GetCollection<Invitation>("invites")
            .Find(item => item.InviteeId == PlayerId.From("friend")).SingleAsync();
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
        root["Reactions"] = new BsonArray { new BsonDocument { ["Me"] = true, ["Emoji"] = "❤️" } };
        var reply = LegacyComment("reply", true);
        reply["EnergySpent"] = 0;
        reply["SubscriberBonus"] = 9;
        reply["OriginCommentId"] = "root";
        var followup = LegacyComment("followup", false);
        followup["ParentCommentId"] = "reply";
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
        await MigrateAsync(database);
        await Assert.That(await ReadAsync(database, "posts", "post")).IsEqualTo(saved);

        // Assert
        await Assert.That(saved["AuthorId"]).IsEqualTo(Author(0, "owner"));
        await Assert.That(saved["WorldId"]).IsEqualTo(new BsonString("world"));
        await Assert.That(saved.Contains("PlayerId") || saved.Contains("ThreadReplies")).IsFalse();
        await Assert.That(comments[0]["AuthorId"]).IsEqualTo(Author(1, "mira"));
        await Assert.That(comments[0]["ParentCommentId"].IsBsonNull).IsTrue();
        await Assert.That(comments[0]["RequestId"].IsBsonNull).IsTrue();
        await Assert.That(comments[0]["Reactions"]).IsEqualTo(new BsonArray { new BsonDocument { ["AuthorId"] = Author(0, "owner"), ["Emoji"] = "❤️" } });
        await Assert.That(comments[1]["AuthorId"]).IsEqualTo(Author(0, "owner"));
        await Assert.That(comments[1]["ParentCommentId"]).IsEqualTo(new BsonString("root"));
        await Assert.That(comments[1]["RequestId"]).IsEqualTo(new BsonString("saved-key"));
        await Assert.That(comments[1]["Reactions"]).IsEqualTo(new BsonArray { new BsonDocument { ["AuthorId"] = Author(1, "mira"), ["Emoji"] = "👏" } });
        await Assert.That(comments[2]["ParentCommentId"]).IsEqualTo(new BsonString("reply"));
        foreach (var comment in comments.Select(value => value.AsBsonDocument))
        {
            await Assert.That(comment.Names.Order(StringComparer.Ordinal)).IsEquivalentTo(new[] { "_id", "AuthorId", "Hidden", "ParentCommentId", "Reactions", "RequestId", "Text" });
        }

        await Assert.That((await ReadAsync(database, "players", "owner"))["EnergyUsed"]).IsEqualTo(new BsonInt32(4));
        await Assert.That((await ReadAsync(database, "worlds", "world"))["Player"]["Subscribers"]).IsEqualTo(new BsonInt32(1000));
    }

    [Test]
    [Arguments("StarId")]
    [Arguments("Avatar")]
    [Arguments("Handle")]
    [Arguments("Author")]
    public async Task Up_LegacyStarIdentity_ResolvesWorldStarAsync(string identityField)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var post = LegacyPost();
        var identity = identityField switch { "Handle" => "@mira", "Author" => "Мира", _ => "mira" };
        var comment = new BsonDocument { ["Id"] = "root", ["Text"] = "Старый комментарий", [identityField] = identity };
        post["Comments"] = new BsonArray { comment };
        await SeedAsync(database, post);

        // Act
        await MigrateAsync(database);

        // Assert
        var saved = await ReadAsync(database, "posts", "post");
        await Assert.That(saved["Comments"][0]["_id"]).IsEqualTo(new BsonString("root"));
        await Assert.That(saved["Comments"][0]["AuthorId"]).IsEqualTo(Author(1, "mira"));
        await Assert.That(saved["Comments"][0]["Reactions"]).IsEqualTo(new BsonArray());
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
        if (existingVersions)
        {
            post["Version"] = 7;
            post["AuthorId"] = NestedAuthor(1, "mira");
            post.Remove("PlayerId");
        }

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

        await database.GetCollection<BsonDocument>("publications").InsertOneAsync(new BsonDocument
        {
            ["_id"] = "publication",
            ["Result"] = new BsonDocument { ["Post"] = post.DeepClone(), ["Subscribers"] = 1234 },
        });
        if (existingVersions)
        {
            await database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", "owner"), Builders<BsonDocument>.Update.Set("Version", 8).Set("EnergyUsed", 4));
            await database.GetCollection<BsonDocument>("worlds").UpdateOneAsync(new BsonDocument("_id", "world"), Builders<BsonDocument>.Update.Set("Version", 9));
        }

        // Act
        await MigrateAsync(database);
        var saved = await ReadAsync(database, "posts", "post");
        var player = await ReadAsync(database, "players", "owner");
        var world = await ReadAsync(database, "worlds", "world");
        var publication = await ReadAsync(database, "publications", "publication");
        await MigrateAsync(database);

        // Assert
        await Assert.That(saved["Comments"]).IsEqualTo(new BsonArray());
        await Assert.That(saved["Version"]).IsEqualTo(new BsonInt32(existingVersions ? 7 : 0));
        await Assert.That(saved["AuthorId"]).IsEqualTo(Author(existingVersions ? 1 : 0, existingVersions ? "mira" : "owner"));
        await Assert.That(player["EnergyUsed"]).IsEqualTo(new BsonInt32(4));
        await Assert.That(player.Contains("PostsUsed")).IsFalse();
        await Assert.That(player["Version"]).IsEqualTo(new BsonInt32(existingVersions ? 8 : 0));
        await Assert.That(world["Version"]).IsEqualTo(new BsonInt32(existingVersions ? 9 : 0));
        await Assert.That(world["Player"]["Subscribers"]).IsEqualTo(new BsonInt32(1000));
        await Assert.That(publication["Result"]["Post"]).IsEqualTo(saved);
        await Assert.That(publication["Result"]["Subscribers"]).IsEqualTo(new BsonInt32(1234));
        await Assert.That(await ReadAsync(database, "posts", "post")).IsEqualTo(saved);
        await Assert.That(await ReadAsync(database, "players", "owner")).IsEqualTo(player);
        await Assert.That(await ReadAsync(database, "worlds", "world")).IsEqualTo(world);
        await Assert.That(await ReadAsync(database, "publications", "publication")).IsEqualTo(publication);
    }

    [Test]
    [Arguments("unknown-author")]
    [Arguments("ambiguous-author")]
    [Arguments("missing-world")]
    [Arguments("foreign-owner")]
    [Arguments("duplicate-comment")]
    [Arguments("duplicate-request")]
    [Arguments("dangling-parent")]
    [Arguments("cycle")]
    [Arguments("usage-conflict")]
    public async Task Up_InvalidLegacyContent_RejectsWithoutPartialDataChangesAsync(string scenario)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var post = LegacyPost();
        post["Comments"] = new BsonArray { LegacyComment("root", false) };
        await SeedAsync(database, post);
        await MakeInvalidAsync(database, post, scenario);
        await database.GetCollection<BsonDocument>("posts").ReplaceOneAsync(new BsonDocument("_id", "post"), post);
        var player = await ReadAsync(database, "players", "owner");
        var world = await ReadAsync(database, "worlds", "world");

        // Act
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => MigrateAsync(database));

        // Assert
        await Assert.That((failure ?? throw new InvalidOperationException("Expected migration failure.")).Message).Contains("Resolve the source data");
        await Assert.That(await ReadAsync(database, "posts", "post")).IsEqualTo(post);
        await Assert.That(await ReadAsync(database, "players", "owner")).IsEqualTo(player);
        await Assert.That(await ReadAsync(database, "worlds", "world")).IsEqualTo(world);
    }

    [Test]
    public async Task Up_TypedWorldStars_ResolvesStoredIdentifierAsync()
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var post = LegacyPost();
        post["Comments"] = new BsonArray { LegacyComment("root", false) };
        await SeedAsync(database, post);
        var template = Common.TestGameData.CreateWorld();
        var world = template with { Id = WorldId.From("world"), Player = template.Player with { PlayerId = PlayerId.From("owner") } };
        var serialized = world.ToBsonDocument();
        await Assert.That(serialized["Stars"][0].AsBsonDocument.Contains("_id")).IsTrue();
        await Assert.That(serialized["Stars"][0]["_id"]).IsEqualTo(new BsonString("mira"));
        await database.GetCollection<BsonDocument>("worlds").ReplaceOneAsync(new BsonDocument("_id", "world"), serialized);

        // Act
        await MigrateAsync(database);

        // Assert
        var saved = await ReadAsync(database, "posts", "post");
        await Assert.That(saved["AuthorId"]).IsEqualTo(Author(0, "owner"));
        await Assert.That(saved["Comments"][0]["AuthorId"]).IsEqualTo(Author(1, "mira"));
        await Assert.That(saved["Comments"][0]["_id"]).IsEqualTo(new BsonString("root"));
        await Assert.That(saved["Comments"][0].AsBsonDocument.Contains("Avatar")).IsFalse();
    }

    [Test]
    [Arguments("legacy")]
    [Arguments("nested")]
    [Arguments("canonical")]
    public async Task Up_MixedAuthorFormats_PreservesCanonicalValuesAcrossRerunAsync(string postFormat)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        const string starId = "mira_Star_ suffix ";
        var post = LegacyPost();
        post["Sentinel"] = new BsonDocument("Untouched", 42);
        post["Version"] = 7;
        if (postFormat != "legacy")
        {
            post["AuthorId"] = postFormat == "nested" ? NestedAuthor(0, "owner") : Author(0, "owner");
        }

        var root = LegacyComment("root", false);
        root["Avatar"] = starId;
        root["Reactions"] = new BsonArray
        {
            new BsonDocument { ["Me"] = true, ["Emoji"] = "❤️" },
            new BsonDocument { ["AuthorId"] = NestedAuthor(1, starId), ["Emoji"] = "👏" },
            new BsonDocument { ["AuthorId"] = Author(1, starId), ["Emoji"] = "✨" },
        };
        var reply = LegacyComment("reply", true);
        reply["AuthorId"] = NestedAuthor(0, "owner");
        reply["ParentCommentId"] = "root";
        reply["RequestId"] = "saved-key";
        var followup = LegacyComment("followup", false);
        followup["AuthorId"] = Author(1, starId);
        followup["ParentCommentId"] = "reply";
        followup["Hidden"] = true;
        post["Comments"] = new BsonArray { root, reply, followup };
        await SeedAsync(database, post);
        await database.GetCollection<BsonDocument>("worlds").UpdateOneAsync(
            new BsonDocument("_id", "world"),
            Builders<BsonDocument>.Update
                .Set("Stars", new BsonArray { new BsonDocument { ["_id"] = starId, ["Name"] = "Мира" } })
                .Set("Version", 9));
        await database.GetCollection<BsonDocument>("publications").InsertOneAsync(new BsonDocument
        {
            ["_id"] = "publication",
            ["Sentinel"] = "envelope",
            ["Result"] = new BsonDocument { ["Post"] = post.DeepClone(), ["Subscribers"] = 1234 },
        });
        var expected = post.DeepClone().AsBsonDocument;
        expected.Remove("PlayerId");
        expected["AuthorId"] = Author(0, "owner");
        expected["Comments"] = new BsonArray
        {
            ExpectedComment("root", BsonNull.Value, BsonNull.Value, Author(1, starId), false, new BsonArray
            {
                new BsonDocument { ["AuthorId"] = Author(0, "owner"), ["Emoji"] = "❤️" },
                new BsonDocument { ["AuthorId"] = Author(1, starId), ["Emoji"] = "👏" },
                new BsonDocument { ["AuthorId"] = Author(1, starId), ["Emoji"] = "✨" },
            }),
            ExpectedComment("reply", "root", "saved-key", Author(0, "owner"), false, []),
            ExpectedComment("followup", "reply", BsonNull.Value, Author(1, starId), true, []),
        };

        // Act
        await MigrateAsync(database);
        var saved = await ReadAsync(database, "posts", "post");
        var publication = await ReadAsync(database, "publications", "publication");
        var player = await ReadAsync(database, "players", "owner");
        var world = await ReadAsync(database, "worlds", "world");
        await MigrateAsync(database);

        // Assert
        await Assert.That(saved).IsEqualTo(expected);
        await Assert.That(publication["Result"]["Post"]).IsEqualTo(expected);
        await Assert.That(publication["Result"]["Subscribers"]).IsEqualTo(new BsonInt32(1234));
        await Assert.That(publication["Sentinel"]).IsEqualTo(new BsonString("envelope"));
        await Assert.That(player["EnergyUsed"]).IsEqualTo(new BsonInt32(4));
        await Assert.That(world["Version"]).IsEqualTo(new BsonInt32(9));
        await Assert.That(world["Player"]["Subscribers"]).IsEqualTo(new BsonInt32(1000));
        await Assert.That(await ReadAsync(database, "posts", "post")).IsEqualTo(saved);
        await Assert.That(await ReadAsync(database, "publications", "publication")).IsEqualTo(publication);
        await Assert.That(await ReadAsync(database, "players", "owner")).IsEqualTo(player);
        await Assert.That(await ReadAsync(database, "worlds", "world")).IsEqualTo(world);
    }

    [Test]
    [Arguments("post", "unknown-prefix")]
    [Arguments("publication-reaction", "foreign-star")]
    public async Task Up_InvalidAuthorLocation_RollsBackAllDocumentsAsync(
        string location,
        string scenario)
    {
        // Arrange
        await using var scope = CreateDatabase();
        var database = scope.Database;
        var valid = LegacyPost();
        await SeedAsync(database, valid);
        var invalid = LegacyPost();
        invalid["_id"] = "later";
        var root = LegacyComment("root", false);
        root["AuthorId"] = Author(1, "mira");
        root["Reactions"] = new BsonArray
            { new BsonDocument { ["AuthorId"] = Author(0, "owner"), ["Emoji"] = "❤️" } };
        invalid["Comments"] = new BsonArray { root };
        invalid["AuthorId"] = Author(0, "owner");
        var invalidAuthor = InvalidAuthor(scenario);
        if (location.EndsWith("post", StringComparison.Ordinal))
        {
            invalid["AuthorId"] = invalidAuthor;
        }
        else if (location.EndsWith("comment", StringComparison.Ordinal))
        {
            root["AuthorId"] = invalidAuthor;
        }
        else
        {
            root["Reactions"][0]["AuthorId"] = invalidAuthor;
        }

        await database.GetCollection<BsonDocument>("publications").InsertOneAsync(new BsonDocument
        {
            ["_id"] = "earlier",
            ["Result"] = new BsonDocument("Post", valid.DeepClone()),
        });
        if (location.StartsWith("publication", StringComparison.Ordinal))
        {
            await database.GetCollection<BsonDocument>("publications").InsertOneAsync(new BsonDocument
            {
                ["_id"] = "later",
                ["Result"] = new BsonDocument("Post", invalid),
            });
        }
        else
        {
            await database.GetCollection<BsonDocument>("posts").InsertOneAsync(invalid);
        }

        if (scenario == "malformed-world-star")
        {
            await database.GetCollection<BsonDocument>("worlds").UpdateOneAsync(
                new BsonDocument("_id", "world"),
                Builders<BsonDocument>.Update.Set(
                    "Stars",
                    new BsonArray { new BsonDocument { ["_id"] = 42, ["Name"] = "Мира" } }));
        }

        var before = await SnapshotAsync(database);

        // Act
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => MigrateAsync(database));

        // Assert
        await Assert.That((failure ?? throw new InvalidOperationException("Expected migration failure.")).Message)
            .Contains("Resolve the source data");
        await Assert.That(await SnapshotAsync(database)).IsEqualTo(before);
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
        await Assert.That(templates.All(world => world.Player.PlayerId.Value == "00000000000000000000000000000000")).IsTrue();
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
        var owned = template.Instantiate(PlayerId.From("seed-player"), DateTimeOffset.UnixEpoch) with
        {
            Player = template.Player with { PlayerId = PlayerId.From("seed-player"), Subscribers = 987 },
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
        Invitation.Create(PlayerId.From("timestamp-inviter"), PlayerId.From("timestamp-friend"), value)
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

    private static IMigrator CreateMigrator(IMongoClient client, string databaseName)
    {
        var options = new RekiMigrationOptions(Options.Create(new MongoOptions { DatabaseName = databaseName }));
        return MigratorBuilder.FromMongoClient(client, options)
            .LoadMigrationsFromAssembly(typeof(InitialGameIndexes).Assembly)
            .Build();
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
        yield return ("worlds", "ix_worlds_player", new BsonDocument("Player.PlayerId", 1), false);
        yield return ("posts", "ix_posts_world_created", new BsonDocument { { "WorldId", 1 }, { "CreatedAtUtc", -1 } }, false);
        yield return ("events", "ix_events_world_day", new BsonDocument { { "PlayerId", 1 }, { "WorldId", 1 }, { "DayStartedAtUtc", 1 } }, false);
        yield return ("threads", "ix_threads_player", new BsonDocument("PlayerId", 1), false);
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

    private static BsonDocument ExpectedComment(
        string id,
        BsonValue parent,
        BsonValue request,
        BsonString author,
        bool hidden,
        BsonArray reactions) => new()
        {
            ["_id"] = id,
            ["ParentCommentId"] = parent,
            ["RequestId"] = request,
            ["AuthorId"] = author,
            ["Text"] = "Комментарий",
            ["Hidden"] = hidden,
            ["Reactions"] = reactions,
        };

    private static BsonValue InvalidAuthor(string scenario) => scenario switch
    {
        "unknown-prefix" => new BsonString("Other_owner"),
        "foreign-star" => Author(1, "other-star"),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
    };

    private static async Task<BsonDocument> SnapshotAsync(IMongoDatabase database)
    {
        var snapshot = new BsonDocument();
        foreach (var collection in new[] { "posts", "publications", "players", "worlds" })
        {
            var documents = await database.GetCollection<BsonDocument>(collection)
                .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync();
            snapshot[collection] = new BsonArray(documents.Select(document => document.DeepClone()));
        }

        return snapshot;
    }

    private static BsonDocument NestedAuthor(int kind, string id) => new() { ["Kind"] = kind, ["Id"] = id };

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
            ["Stars"] = new BsonArray { new BsonDocument { ["Id"] = "mira", ["Name"] = "Мира" } },
        });
        await database.GetCollection<BsonDocument>("players").InsertOneAsync(new BsonDocument { ["_id"] = "owner", ["PostsUsed"] = 4 });
        await database.GetCollection<BsonDocument>("posts").InsertOneAsync(post);
    }

    private static Task<BsonDocument> ReadAsync(IMongoDatabase database, string collection, string id) =>
        database.GetCollection<BsonDocument>(collection).Find(new BsonDocument("_id", id)).SingleAsync();

    private static async Task MakeInvalidAsync(IMongoDatabase database, BsonDocument post, string scenario)
    {
        var root = post["Comments"][0].AsBsonDocument;
        switch (scenario)
        {
            case "unknown-author":
            {
                root["Avatar"] = "unknown";
                root["Author"] = "Неизвестно";
                break;
            }

            case "ambiguous-author":
            {
                root.Remove("Avatar");
                await database.GetCollection<BsonDocument>("worlds").UpdateOneAsync(new BsonDocument("_id", "world"), Builders<BsonDocument>.Update.Push("Stars", new BsonDocument { ["Id"] = "other", ["Name"] = "Мира" }));
                break;
            }

            case "missing-world":
            {
                post["WorldId"] = "missing";
                break;
            }

            case "foreign-owner":
            {
                post["PlayerId"] = "other-player";
                break;
            }

            case "duplicate-comment":
            {
                post["Comments"].AsBsonArray.Add(root.DeepClone());
                break;
            }

            case "duplicate-request":
            {
                root["RequestId"] = "duplicate";
                var reply = LegacyComment("reply", true);
                reply["RequestId"] = "duplicate";
                post["Comments"].AsBsonArray.Add(reply);
                break;
            }

            case "dangling-parent":
            {
                root["ParentCommentId"] = "missing";
                break;
            }

            case "cycle":
            {
                root["ParentCommentId"] = "reply";
                var reply = LegacyComment("reply", true);
                reply["ParentCommentId"] = "root";
                post["Comments"].AsBsonArray.Add(reply);
                break;
            }

            case "usage-conflict":
            {
                await database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", "owner"), Builders<BsonDocument>.Update.Set("EnergyUsed", 5));
                break;
            }

            default:
            {
                throw new ArgumentOutOfRangeException(nameof(scenario));
            }
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

    private sealed class MigrationDatabase(IMongoClient client) : IAsyncDisposable
    {
        public IMongoDatabase Database { get; } = client.GetDatabase($"migration_{Guid.NewGuid():N}");

        public ValueTask DisposeAsync() => new(client.DropDatabaseAsync(Database.DatabaseNamespace.DatabaseName, CancellationToken.None));
    }
}
