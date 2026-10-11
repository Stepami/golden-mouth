using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Infrastructure.Health;

internal sealed class MongoHealthCheck(IMongoDatabase database)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new BsonDocument("ping", 1);
            await database.RunCommandAsync<BsonDocument>(
                command,
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("MongoDB did not answer the readiness probe.");
        }
    }
}