using System.Security.Cryptography;
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
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Reki.Tests;

public sealed class TestSessionFixture : IAsyncInitializer, IAsyncDisposable
{
    public const long TelegramBotId = 123456;

    private readonly MongoFixture _mongo = new();
    private readonly WireMockServer _classifier = WireMockServer.Start();
    private MongoClient? _client;
    private OpenAiStub? _llm;

    public string ConnectionString => _mongo.ConnectionString;

    public string DatabaseName { get; } = $"reki_tests_{Guid.NewGuid():N}";

    public IMongoClient MongoClient => _client ?? throw new InvalidOperationException("The test session has not been initialized.");

    public IMongoDatabase Database => MongoClient.GetDatabase(DatabaseName);

    public IReadOnlyDictionary<string, World> LegacyWorldTemplates { get; } =
        SeedWorldTemplates.CreateTemplates().Zip(new[] { "stage", "academy", "it", "stream", "own" })
            .ToDictionary(pair => pair.Second, pair => pair.First, StringComparer.Ordinal);

    public OpenAiStub Llm => _llm ??= new OpenAiStub();

    public string ClassifierUrl => _classifier.Url ?? throw new InvalidOperationException("Classifier stub has no URL.");

    public FakeTimeProvider Clock { get; } = new(
        new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));

    public string SigningKey { get; } = CreateSigningKey();

    public string EncryptionKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string TelegramBotToken { get; } = $"{TelegramBotId}:{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        _classifier.Given(Request.Create().WithPath("/classify").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody("""{"data":[[{"label":"insult","score":0.05},{"label":"anger","score":0.05},{"label":"sadness","score":0.05},{"label":"fear","score":0.05}]]}"""));

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
                _classifier.Dispose();
            }
            finally
            {
                await _mongo.DisposeAsync();
            }
        }
    }

    private static string CreateSigningKey()
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportRSAPrivateKeyPem();
    }
}
