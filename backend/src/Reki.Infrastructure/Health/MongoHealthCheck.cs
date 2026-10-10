using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Persistence;

namespace Reki.Infrastructure.Health;

internal sealed class MongoHealthCheck(
    IMongoClient mongoClient,
    IOptions<MongoOptions> options)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = mongoClient.GetDatabase(options.Value.DatabaseName);
            var command = new BsonDocument("ping", 1);
            await database.RunCommandAsync<BsonDocument>(
                command,
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (MongoException)
        {
            return CreateUnhealthyResult();
        }
        catch (TimeoutException)
        {
            return CreateUnhealthyResult();
        }
    }

    private static HealthCheckResult CreateUnhealthyResult()
    {
        return HealthCheckResult.Unhealthy(
            "MongoDB did not answer the readiness probe.");
    }
}