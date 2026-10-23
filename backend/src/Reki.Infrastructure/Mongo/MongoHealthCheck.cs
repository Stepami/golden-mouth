using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;

namespace Reki.Infrastructure.Mongo;

internal sealed class MongoHealthCheck(MongoCompositionRoot compositionRoot) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new BsonDocument("ping", 1);
            await compositionRoot.MongoDatabase.RunCommandAsync<BsonDocument>(
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