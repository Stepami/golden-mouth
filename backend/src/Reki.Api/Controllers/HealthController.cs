using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Reki.Api.Controllers;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("health")]
public sealed class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet("live")]
    public Task<Results<Ok, StatusCodeHttpResult>> GetLivenessAsync(
        CancellationToken cancellationToken)
    {
        return CheckHealthAsync(
            registration => registration.Tags.Contains("live"),
            cancellationToken);
    }

    [HttpGet("ready")]
    public Task<Results<Ok, StatusCodeHttpResult>> GetReadinessAsync(
        CancellationToken cancellationToken)
    {
        return CheckHealthAsync(_ => true, cancellationToken);
    }

    private async Task<Results<Ok, StatusCodeHttpResult>> CheckHealthAsync(
        Func<HealthCheckRegistration, bool> predicate,
        CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(
            predicate,
            cancellationToken);

        if (report.Status == HealthStatus.Unhealthy)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return TypedResults.Ok();
    }
}
