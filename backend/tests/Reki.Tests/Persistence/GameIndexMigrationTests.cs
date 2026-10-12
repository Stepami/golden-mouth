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
        var database = mongo.MongoClient.GetDatabase($"nested_worlds_{Guid.NewGuid():N}");
        try
        {
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
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    public async Task Migrate_EmptyDatabase_CreatesCurrentIndexesAsync()
    {
        // Arrange
        var client = mongo.MongoClient;
        var database = client.GetDatabase($"index_migrations_{Guid.NewGuid():N}");
        try
        {
            var migrator = CreateMigrator(client, database.DatabaseNamespace.DatabaseName);

            // Act
            await migrator.MigrateAsync(cancellationToken: CancellationToken.None);

            // Assert
            await AssertCurrentIndexesAsync(database);
            var playerCount = await database.GetCollection<BsonDocument>("players")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
            await Assert.That(playerCount).IsEqualTo(0L);
        }
        finally
        {
            await client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, CancellationToken.None);
        }
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, true)]
    public async Task Migrate_ExistingDatabase_SafelyAdoptsCurrentStateAsync(
        bool existingIndexes,
        bool legacyIndex)
    {
        // Arrange
        var client = mongo.MongoClient;
        var database = client.GetDatabase($"index_migrations_{Guid.NewGuid():N}");
        try
        {
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

            var migrator = CreateMigrator(client, database.DatabaseNamespace.DatabaseName);

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
        finally
        {
            await client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, CancellationToken.None);
        }
    }

    [Test]
    [Arguments("stage")]
    [Arguments("stream")]
    public async Task Migrate_TemplateWorld_ConsolidatesPlayerDataAsync(string kind)
    {
        var client = mongo.MongoClient;
        var database = client.GetDatabase($"world_migration_{Guid.NewGuid():N}");
        try
        {
            using var session = await client.StartSessionAsync();
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

            var migrator = CreateMigrator(client, database.DatabaseNamespace.DatabaseName);
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
        finally
        {
            await client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, CancellationToken.None);
        }
    }

    [Test]
    public async Task RepairLegacyDocuments_MissingValues_PreservesExistingDataAsync()
    {
        var client = mongo.MongoClient;
        var database = client.GetDatabase($"document_arrays_{Guid.NewGuid():N}");
        try
        {
            using var session = await client.StartSessionAsync(cancellationToken: CancellationToken.None);
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
                PlayerId.From("player").ToAuthor(),
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
        finally
        {
            await client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, CancellationToken.None);
        }
    }

    [Test]
    public async Task MoveReferralState_LegacyCredits_PreservesAttributionWithoutDoubleCountingAsync()
    {
        // Arrange
        var database = mongo.MongoClient.GetDatabase($"referral_migration_{Guid.NewGuid():N}");
        try
        {
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
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    [Arguments("attribution")]
    [Arguments("created")]
    public async Task MoveReferralState_IncompleteLegacyHistory_CompletesWithFallbacksAsync(string missing)
    {
        // Arrange
        var database = mongo.MongoClient.GetDatabase($"referral_invalid_{Guid.NewGuid():N}");
        try
        {
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
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    public async Task MoveReferralState_UnknownFirstPostDate_PreservesHistoricalBonusUntilResetAsync()
    {
        // Arrange
        var database = mongo.MongoClient.GetDatabase($"referral_unknown_{Guid.NewGuid():N}");
        try
        {
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
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
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
}