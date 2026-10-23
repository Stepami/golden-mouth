using Kot.MongoDB.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs;
using Reki.Infrastructure.Mongo;
using Reki.Infrastructure.Mongo.Migrations;
using TickerQ.DependencyInjection;
using TickerQ.MongoDB.DependencyInjection;
using TickerQ.Utilities.Entities;
using TUnit.Core.Interfaces;

namespace Reki.Tests;

public sealed class TestSessionFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly MongoFixture _mongo = new();
    private MongoClient? _client;
    private OpenAiStub? _llm;

    public string ConnectionString => _mongo.ConnectionString;

    public string DatabaseName { get; } = $"reki_tests_{Guid.NewGuid():N}";

    public IMongoClient MongoClient => _client ?? throw new InvalidOperationException("The test session has not been initialized.");

    public IMongoDatabase Database => MongoClient.GetDatabase(DatabaseName);

    public IReadOnlyDictionary<string, World> LegacyWorldTemplates { get; } =
        SeedWorldTemplates.CreateTemplates().ToDictionary(template => template.BalanceKey, StringComparer.Ordinal);

    public OpenAiStub Llm => _llm ??= new OpenAiStub();

    public FakeTimeProvider Clock { get; } = new(
        new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        // Match production registration order, including TickerQ's BSON UUID serializer.
        new ServiceCollection().AddTickerQ<RekiTimeTicker, CronTickerEntity>(options =>
            options.AddOperationalStore(store => store.UseExistingMongoClient(DatabaseName)));
        MongoClassMappings.Register();
        await _mongo.InitializeAsync();
        _client = new MongoClient(ConnectionString);
    }

    public async Task ClearGameDataAsync(CancellationToken cancellationToken)
    {
        var options = new MigrationOptions(DatabaseName);
        using var cursor = await Database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        var collections = await cursor.ToListAsync(cancellationToken);
        await Task.WhenAll(collections.Where(name =>
                !name.StartsWith("ticker_", StringComparison.Ordinal) && name != "world_templates" &&
                name != options.MigrationsCollectionName && name != options.MigrationsLockCollectionName)
            .Select(collection => Database.GetCollection<BsonDocument>(collection)
                .DeleteManyAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken)));
    }

    public void ResetExternalState()
    {
        _llm?.Reset();
        Clock.AdjustTime(Clock.Start);
    }

    public async ValueTask DisposeAsync()
    {
        // TUnit disposes the dependent ApiFactory before this nested session fixture.
        try
        {
            if (_client is not null)
            {
                await _client.DropDatabaseAsync(DatabaseName, CancellationToken.None);
            }
        }
        finally
        {
            try
            {
                _llm?.Dispose();
            }
            finally
            {
                await _mongo.DisposeAsync();
            }
        }
    }
}
