using MongoDB.Driver;
using Reki.Infrastructure.Persistence;
using Serilog;

namespace Reki.Infrastructure.Logging;

public static class MongoLoggingExtensions
{
    private static readonly TimeSpan LogRetention = TimeSpan.FromDays(30);

    public static LoggerConfiguration WriteToMongoDb(
        this LoggerConfiguration loggerConfiguration,
        IMongoClient mongoClient,
        MongoOptions options)
    {
        return loggerConfiguration.WriteTo.Logger(mongoLogger => mongoLogger
            .Filter.ByIncludingOnly(MongoLogEventFilter.ShouldPersist)
            .WriteTo.MongoDBBson(sink =>
            {
                IMongoDatabase database = mongoClient.GetDatabase(options.DatabaseName);
                sink.SetMongoDatabase(database);
                sink.SetCollectionName(options.LogCollectionName);
                sink.SetExpireTTL(LogRetention);
            }));
    }
}
