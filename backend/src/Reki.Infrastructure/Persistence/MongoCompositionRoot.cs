using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence;

internal sealed class MongoCompositionRoot
{
    private static readonly TimeSpan MongoServerSelectionTimeout = TimeSpan.FromSeconds(5);

    public MongoCompositionRoot(IOptions<MongoOptions> options)
    {
        var settings = MongoClientSettings.FromConnectionString(
            options.Value.ConnectionString);
        settings.ServerSelectionTimeout = MongoServerSelectionTimeout;
        MongoClient = new MongoClient(settings);
        MongoDatabase = MongoClient.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoClient MongoClient { get; }

    public IMongoDatabase MongoDatabase { get; }
}