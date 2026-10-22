using DotNet.Testcontainers.Builders;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using TUnit.Core.Interfaces;

namespace Reki.Tests;

public sealed class MongoFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string InitializeReplicaSet = """
        try {
            rs.status();
        } catch (error) {
            if (error.codeName !== "NotYetInitialized") throw error;
            rs.initiate({_id: "rs0", members: [{_id: 0, host: "127.0.0.1:27017"}]});
        }
        quit(db.hello().isWritablePrimary ? 0 : 1);
        """;

    // Bypass the image entrypoint's temporary mongod and start only the replica-set process.
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8.0.32-noble")
        .WithUsername(string.Empty)
        .WithPassword(string.Empty)
        .WithEntrypoint("mongod")
        .WithCommand("--replSet", "rs0", "--bind_ip_all")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
            ["mongosh", "--quiet", "--eval", InitializeReplicaSet],
            strategy => strategy.WithTimeout(TimeSpan.FromMinutes(1))))
        .Build();

    public string ConnectionString => new MongoUrlBuilder(_container.GetConnectionString())
    {
        DirectConnection = true,
        ReplicaSetName = "rs0",
    }.ToString();

    public Task InitializeAsync() => _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
