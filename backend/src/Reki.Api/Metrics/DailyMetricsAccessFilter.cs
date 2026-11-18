using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Reki.Application.Metrics.Responses;

namespace Reki.Api.Metrics;

internal sealed class DailyMetricsAccessFilter(IOptionsMonitor<DailyMetricsAccessOptions> options)
    : IAsyncResourceFilter, IDisposable
{
    private readonly FixedWindowRateLimiter _limiter = new(new FixedWindowRateLimiterOptions
    {
        PermitLimit = 10,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
    });

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var token = options.CurrentValue.Token;
        var authorization = context.HttpContext.Request.Headers.Authorization;
        if (token.Length < 32 || authorization.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(authorization[0] ?? string.Empty),
                Encoding.UTF8.GetBytes($"Bearer {token}")))
        {
            context.Result = new ObjectResult(new MetricsErrorResponse("Invalid or missing bearer token."))
            {
                StatusCode = StatusCodes.Status401Unauthorized,
            };
            return;
        }

        using var lease = _limiter.AttemptAcquire();
        if (!lease.IsAcquired)
        {
            context.Result = new ObjectResult(new MetricsErrorResponse("Rate limit exceeded."))
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
            return;
        }

        context.HttpContext.Response.Headers.CacheControl = "no-store";
        await next();
    }

    public void Dispose() => _limiter.Dispose();
}
