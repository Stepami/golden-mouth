using TUnit.Core.Interfaces;
using Testcontainers.MongoDb;

namespace Reki.Tests;

public sealed class MongoFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:9.0.4-resolute")
        .WithReplicaSet()
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
