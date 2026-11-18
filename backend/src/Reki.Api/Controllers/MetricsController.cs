using System.Globalization;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Metrics;
using Reki.Application.Exceptions;
using Reki.Application.Metrics.GetDailyMetrics;
using Reki.Application.Metrics.Responses;

namespace Reki.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("metrics")]
[ServiceFilter<DailyMetricsAccessFilter>]
public sealed class MetricsController(ISender sender) : ControllerBase
{
    /// <summary>Read verified product metrics for inclusive Moscow dates.</summary>
    /// <param name="dateFrom">First date, in YYYY-MM-DD format.</param>
    /// <param name="dateTo">Last date, in YYYY-MM-DD format.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Verified daily aggregates or a safe error.</returns>
    [HttpGet("daily", Name = "GetDailyMetrics")]
    public async Task<Results<Ok<DailyMetricsResponse>, BadRequest<MetricsErrorResponse>, JsonHttpResult<MetricsErrorResponse>>> GetDailyMetricsAsync(
        [FromQuery(Name = "date_from")] string? dateFrom,
        [FromQuery(Name = "date_to")] string? dateTo,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from) ||
            !DateOnly.TryParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to) ||
            to < from || to.DayNumber - from.DayNumber >= 90)
        {
            return TypedResults.BadRequest(new MetricsErrorResponse("Use YYYY-MM-DD dates and an inclusive range of 1 to 90 days."));
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(9));
        try
        {
            return TypedResults.Ok(await sender.Send(new DailyMetricsQuery(from, to), timeout.Token));
        }
        catch (MetricsUnavailableException)
        {
            return TypedResults.Json(new MetricsErrorResponse("Verified metrics are unavailable for the requested range."), statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return TypedResults.Json(new MetricsErrorResponse("Metrics source timed out."), statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException or OperationCanceledException))
        {
            return TypedResults.Json(new MetricsErrorResponse("Metrics source is unavailable."), statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
