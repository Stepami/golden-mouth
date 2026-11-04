using Testcontainers.MongoDb;
using TUnit.Core.Interfaces;

namespace Reki.Tests;

public sealed class MongoFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly Lazy<MongoDbContainer> _container = new(() => new MongoDbBuilder("mongo:9.0.4-resolute")
        .WithReplicaSet()
        .WithCommand("--setParameter", "ttlMonitorEnabled=false")
        .Build());

    private readonly string? _externalConnection = Environment.GetEnvironmentVariable("REKI_TEST_MONGO_CONNECTION");

    public string ConnectionString => _externalConnection ?? _container.Value.GetConnectionString();

    public Task InitializeAsync() => _externalConnection is null ? _container.Value.StartAsync() : Task.CompletedTask;

    public ValueTask DisposeAsync() => _externalConnection is null ? _container.Value.DisposeAsync() : ValueTask.CompletedTask;
}
