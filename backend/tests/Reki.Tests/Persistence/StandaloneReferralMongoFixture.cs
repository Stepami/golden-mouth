using Testcontainers.MongoDb;
using TUnit.Core.Interfaces;

namespace Reki.Tests.Persistence;

public sealed class StandaloneReferralMongoFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8.0.32-noble").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
