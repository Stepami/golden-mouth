using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using TUnit.Core.Interfaces;

namespace Reki.Tests;

public sealed class MongoFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:9.0.4-resolute")
        .WithReplicaSet()
        .WithCommand(
            "--setParameter",
            "ttlMonitorEnabled=false",
            "--setParameter",
            "mongotHost=localhost:27028",
            "--setParameter",
            "searchIndexManagementHostAndPort=localhost:27028",
            "--setParameter",
            "skipAuthenticationToSearchIndexManagementServer=true",
            "--setParameter",
            "useGrpcForSearch=true",
            "--setParameter",
            "searchTLSMode=disabled")
        .Build();

    private IContainer? _search;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var password = Guid.NewGuid().ToString("N");
        var client = new MongoClient(ConnectionString);
        await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "createUser", "mongot" },
            { "pwd", password },
            { "roles", new BsonArray { "searchCoordinator" } },
        });

        const string configuration = """
            syncSource:
              replicaSet:
                hostAndPort: "localhost:27017"
                scramAuth:
                  username: "mongot"
                  passwordFile: "/secrets/passwordFile"
                  authSource: "admin"
                  tls:
                    enabled: false
              replicationReader:
                readPreference: "primary"
            storage:
              dataPath: "/data/mongot"
            server:
              grpc:
                address: "0.0.0.0:27028"
                tls:
                  mode: "disabled"
            healthCheck:
              address: "0.0.0.0:8080"
            logging:
              verbosity: INFO
            """;

        // Share the replica set's loopback address without changing its advertised hostname.
        _search = new ContainerBuilder("mongodb/mongodb-community-search:1.70.1")
            .WithCreateParameterModifier(parameters =>
            {
                parameters.HostConfig ??= new();
                parameters.HostConfig.NetworkMode = $"container:{_container.Id}";
            })
            .WithResourceMapping(Encoding.UTF8.GetBytes(configuration), "/mongot-community/config.default.yml")
            .WithResourceMapping(Encoding.UTF8.GetBytes(password), "/secrets/passwordFile", fileMode: UnixFileModes.UserRead)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("curl", "-fsS", "http://localhost:8080/health"))
            .Build();
        await _search.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_search is not null)
        {
            await _search.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
