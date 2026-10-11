using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence;

internal sealed class MongoCompositionRoot
{
    private static readonly TimeSpan MongoServerSelectionTimeout = TimeSpan.FromSeconds(5);

    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _mongoDatabase;

    public MongoCompositionRoot(IOptions<MongoOptions> options)
    {
        var settings = MongoClientSettings.FromConnectionString(
            options.Value.ConnectionString);
        settings.ServerSelectionTimeout = MongoServerSelectionTimeout;
        _mongoClient = new MongoClient(settings);
        _mongoDatabase = _mongoClient.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoClient MongoClient => _mongoClient;

    public IMongoDatabase MongoDatabase => _mongoDatabase;
}