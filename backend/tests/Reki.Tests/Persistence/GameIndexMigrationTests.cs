using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Persistence;
using Reki.Infrastructure.Persistence.Migrations;

namespace Reki.Tests.Persistence;

[ClassDataSource<TestSessionFixture>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Migrations", Order = 0)]
public sealed class GameIndexMigrationTests(TestSessionFixture mongo)
{
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
    [Arguments(false, true)]
    [Arguments(true, false)]
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
            await Assert.That(savedPlayer).IsEqualTo(player);
            await Assert.That(savedChallenge).IsEqualTo(challenge);
            var options = new MigrationOptions(database.DatabaseNamespace.DatabaseName);
            var historyCount = await database.GetCollection<BsonDocument>(options.MigrationsCollectionName)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
            await Assert.That(historyCount).IsEqualTo(2L);
        }
        finally
        {
            await client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, CancellationToken.None);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Migrate_ConflictingDefinition_StopsWithoutDroppingIndexAsync(bool legacy)
    {
        // Arrange
        var client = mongo.MongoClient;
        var database = client.GetDatabase($"index_migrations_{Guid.NewGuid():N}");
        try
        {
            var collection = legacy ? "challenges" : "players";
            var name = legacy ? "ux_challenges_active_player" : "ux_players_telegram_user";
            await CreateIndexAsync(database, collection, name, new BsonDocument("UnexpectedField", 1), unique: true);
            var migrator = CreateMigrator(client, database.DatabaseNamespace.DatabaseName);

            // Act
            Exception? failure = null;
            try
            {
                await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            // Assert
            await Assert.That(failure).IsNotNull();
            using var cursor = await database.GetCollection<BsonDocument>(collection).Indexes
                .ListAsync(CancellationToken.None);
            var indexes = await cursor.ToListAsync(CancellationToken.None);
            var preserved = indexes.Single(index => index["name"] == name);
            await Assert.That(preserved["key"]).IsEqualTo(new BsonDocument("UnexpectedField", 1));
            var options = new MigrationOptions(database.DatabaseNamespace.DatabaseName);
            var historyCount = await database.GetCollection<BsonDocument>(options.MigrationsCollectionName)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
            await Assert.That(historyCount).IsEqualTo(legacy ? 1L : 0L);

            // Resolve the conflict and resume after a partially applied migration.
            await database.GetCollection<BsonDocument>(collection).Indexes.DropOneAsync(name, CancellationToken.None);
            await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
            await AssertCurrentIndexesAsync(database);
        }
        finally
        {
            await client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, CancellationToken.None);
        }
    }

    [Test]
    public async Task Migrate_ReplacementCannotBeCreated_PreservesCurrentStateAsync()
    {
        // Arrange
        var client = mongo.MongoClient;
        var database = client.GetDatabase($"index_migrations_{Guid.NewGuid():N}");
        try
        {
            var challenges = database.GetCollection<BsonDocument>("challenges");
            await challenges.InsertManyAsync(
            [
                new BsonDocument
                {
                    { "_id", "first" }, { "PlayerId", "first" }, { "Participants", new BsonArray { "shared" } },
                    { "Status", 1 },
                },
                new BsonDocument
                {
                    { "_id", "second" }, { "PlayerId", "second" }, { "Participants", new BsonArray { "shared" } },
                    { "Status", 1 },
                },
            ],
            cancellationToken: CancellationToken.None);
            await CreateIndexAsync(database, "challenges", "ux_challenges_active_player", new BsonDocument("PlayerId", 1), unique: true);

            // Act
            Exception? failure = null;
            try
            {
                await CreateMigrator(client, database.DatabaseNamespace.DatabaseName)
                    .MigrateAsync(cancellationToken: CancellationToken.None);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            // Assert
            await Assert.That(failure).IsNotNull();
            using var cursor = await challenges.Indexes.ListAsync(CancellationToken.None);
            var indexes = await cursor.ToListAsync(CancellationToken.None);
            await Assert.That(indexes.Any(index => index["name"] == "ux_challenges_active_player")).IsTrue();
            await Assert.That(await challenges.CountDocumentsAsync(
                FilterDefinition<BsonDocument>.Empty,
                cancellationToken: CancellationToken.None)).IsEqualTo(2L);
        }
        finally
        {
            await client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, CancellationToken.None);
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
        yield return ("players", "ix_players_world", new BsonDocument("World", 1), false);
        yield return ("posts", "ix_posts_player_created",
            new BsonDocument { { "PlayerId", 1 }, { "CreatedAtUtc", -1 } }, false);
        yield return ("publications", "ix_publications_expiry", new BsonDocument("ExpiresAtUtc", 1), false);
        yield return ("stars_memory", "ux_stars_memory_player_star",
            new BsonDocument { { "PlayerId", 1 }, { "StarId", 1 } }, true);
        yield return ("events", "ix_events_player_day",
            new BsonDocument { { "PlayerId", 1 }, { "DayStartedAtUtc", -1 } }, false);
        yield return ("challenges", "ux_challenges_active_participants", new BsonDocument("Participants", 1), true);
        yield return ("worlds", "ux_worlds_player_world", new BsonDocument { { "PlayerId", 1 }, { "WorldId", 1 } },
            true);
        yield return ("threads", "ix_threads_player", new BsonDocument("PlayerId", 1), false);
        yield return ("invites", "ix_invites_player", new BsonDocument("PlayerId", 1), false);
    }

    private static async Task SeedCurrentIndexesAsync(IMongoDatabase database)
    {
        foreach (var (collection, name, keys, unique) in ExpectedIndexes())
        {
            await CreateIndexAsync(database, collection, name, keys, unique);
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