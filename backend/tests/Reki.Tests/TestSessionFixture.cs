using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Time.Testing;
using MongoDB.Bson;
using MongoDB.Driver;
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

    public OpenAiStub Llm => _llm ??= new OpenAiStub();

    public FakeTimeProvider Clock { get; } = new(
        new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await _mongo.InitializeAsync();
        _client = new MongoClient(ConnectionString);
    }

    public async Task ClearGameDataAsync(CancellationToken cancellationToken)
    {
        var options = new MigrationOptions(DatabaseName);
        using var cursor = await Database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        var collections = await cursor.ToListAsync(cancellationToken);
        foreach (var collection in collections.Where(name =>
                     name != options.MigrationsCollectionName && name != options.MigrationsLockCollectionName))
        {
            await Database.GetCollection<BsonDocument>(collection)
                .DeleteManyAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken);
        }
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