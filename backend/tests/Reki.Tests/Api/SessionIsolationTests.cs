using Kot.MongoDB.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class SessionIsolationTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task Reset_PopulatedSession_PreservesSharedInfrastructureAsync()
    {
        // Arrange
        const long telegramUserId = 9301;
        using var api = Factory.CreateApiClient(telegramUserId);
        await CreatePlayerWithWorldAsync(api);
        var generation = await GenerateAsync(telegramUserId, "пост для проверки изоляции");
        await Assert.That(generation.Body).Contains("\"type\":\"ready_for_moderation\"");
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(1);
        await Assert.That(Factory.Llm.MappingCount).IsEqualTo(2);
        var server = Factory.Server;
        var stub = Factory.Llm;
        var clock = Factory.Services.GetRequiredService<TimeProvider>();
        Factory.Session.Clock.Advance(TimeSpan.FromHours(1));
        await Assert.That(clock.GetUtcNow()).IsEqualTo(Factory.Session.Clock.Start.AddHours(1));
        var transientState = Database.GetCollection<BsonDocument>("test_transient_state");
        await transientState.InsertOneAsync(new BsonDocument("state", 1), cancellationToken: CancellationToken.None);
        var migrationOptions = new MigrationOptions(Factory.DatabaseName);
        var migrations = Database.GetCollection<BsonDocument>(migrationOptions.MigrationsCollectionName);
        var migrationLocks = Database.GetCollection<BsonDocument>(migrationOptions.MigrationsLockCollectionName);
        var historyCount = await migrations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
        await migrationLocks.InsertOneAsync(new BsonDocument("_id", "cleanup-probe"), cancellationToken: CancellationToken.None);
        var lockCount = await migrationLocks.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
        await Assert.That(historyCount).IsEqualTo(6L);
        await Assert.That(lockCount).IsEqualTo(1L);

        // Act
        await Factory.Session.ClearGameDataAsync(CancellationToken.None);
        Factory.Session.ResetExternalState();

        // Assert
        foreach (var name in new[] { "players", "publications", "test_transient_state" })
        {
            var count = await Database.GetCollection<BsonDocument>(name)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
            await Assert.That(count).IsEqualTo(0L);
        }

        await Assert.That(await migrations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None)).IsEqualTo(historyCount);
        await Assert.That(await migrationLocks.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None)).IsEqualTo(lockCount);
        using var indexesCursor = await Database.GetCollection<BsonDocument>("players").Indexes.ListAsync(CancellationToken.None);
        var indexes = await indexesCursor.ToListAsync(CancellationToken.None);
        var telegramIndex = indexes.Single(index => index["name"] == "ux_players_telegram_user");
        await Assert.That(telegramIndex["key"]).IsEqualTo(new BsonDocument("TelegramUserId", 1));
        await Assert.That(telegramIndex["unique"].AsBoolean).IsTrue();
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(0);
        await Assert.That(Factory.Llm.MappingCount).IsEqualTo(0);
        await Assert.That(clock.GetUtcNow()).IsEqualTo(Factory.Session.Clock.Start);
        await migrationLocks.DeleteOneAsync(new BsonDocument("_id", "cleanup-probe"), CancellationToken.None);
        using var secondApi = Factory.CreateApiClient(telegramUserId);
        await CreatePlayerWithWorldAsync(secondApi);
        await Assert.That(ReferenceEquals(Factory.Server, server)).IsTrue();
        await Assert.That(ReferenceEquals(Factory.Llm, stub)).IsTrue();
    }
}