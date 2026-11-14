using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TickerQ.Utilities.Licensing;

namespace Reki.Infrastructure.Jobs;

internal sealed class TickerQLicenseHealthCheck(
    IOptions<JobsOptions> options,
    TickerQLicenseStateProvider license) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(!options.Value.ExecutionEnabled || license.ExecutionAllowed
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("TickerQ license is unavailable."));
}
