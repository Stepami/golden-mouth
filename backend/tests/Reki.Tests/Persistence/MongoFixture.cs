using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;
using TUnit.Core.Interfaces;

namespace Reki.Tests.Persistence;

public sealed class MongoFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly IContainer _container = new ContainerBuilder("mongodb/mongodb-atlas-local:8.3.11-20261103T091247Z")
        .WithPortBinding(27017, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())
        .Build();

    public string ConnectionString => $"mongodb://{_container.Hostname}:{_container.GetMappedPublicPort(27017)}/?directConnection=true";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(ConnectionString);
        await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "setParameter", 1 },
            { "ttlMonitorEnabled", false },
        });
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
