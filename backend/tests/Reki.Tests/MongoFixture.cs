using Testcontainers.MongoDb;
using TUnit.Core.Interfaces;

namespace Reki.Tests;

public sealed class MongoFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8.0.32-noble")
        .WithReplicaSet()
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}