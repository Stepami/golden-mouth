using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reki.Api.Requests.Telemetry;
using Reki.Api.Requests.Telemetry.AnalyticsBatch;
using Reki.Api.Requests.Telemetry.ClientLogBatch;
using Reki.Application.RekiContext;
using Reki.Domain.Telemetry;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.Policies.RekiAppJwt)]
[Produces("application/json")]
[Route("/")]
public sealed partial class TelemetryController(
    ISender sender,
    IRekiContext rekiContext,
    ILogger<TelemetryController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("sessions", Name = "StartSession")]
    [ActionName("StartSession")]
    public SessionId StartSession() => SessionId.New();

    [AllowAnonymous]
    [EnableRateLimiting("landing-entry")]
    [RequestSizeLimit(32768)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("events", Name = "SubmitAnalytics")]
    [ActionName("SubmitAnalytics")]
    public async Task<ActionResult> SubmitAnalyticsAsync(
        AnalyticsBatchRequest request,
        CancellationToken cancellationToken)
    {
        var anonymous = User.Identity?.IsAuthenticated != true;
        if (anonymous && Request.Headers.Authorization.Count > 0)
        {
            return Unauthorized();
        }

        if (anonymous && (Request.ContentLength > 8192 || request.Events.Count is < 1 or > 5 ||
            request.Platform != "web" || request.Experiments.Count != 0 ||
            request.Events.Any(entry => entry.WorldId is not null || entry.Screen != "landing" || entry.Prev is not null ||
                entry.Name is not ("landing_view" or "landing_continue" or "landing_auth_complete" or
                    "landing_world_selected" or "landing_character_selected" or "landing_reaction_viewed"))))
        {
            return BadRequest();
        }

        await sender.Send(request.ToSubmitAnalytics() with { Anonymous = anonymous }, cancellationToken);
        return NoContent();
    }

    [HttpPost("logs", Name = "SubmitClientLogs")]
    [ActionName("SubmitClientLogs")]
    public Task<NoContent> SubmitClientLogsAsync(
        ClientLogBatchRequest request,
        CancellationToken cancellationToken)
    {
        var playerId = rekiContext.PlayerId;
        foreach (var entry in request.Entries)
        {
            var properties = entry.Props.SanitizeProperties();
            properties.TryGetValue("status", out var status);
            properties.TryGetValue("path", out var path);
            WriteClientLog(
                entry.Level,
                entry.Event,
                playerId.Value.ToString("N"),
                entry.Screen ?? string.Empty,
                status ?? string.Empty,
                path ?? string.Empty);
        }

        return Task.FromResult(TypedResults.NoContent());
    }

    [HttpPost("reports", Name = "ReportComment")]
    [ActionName("ReportComment")]
    public async Task<Ok> ReportCommentAsync(
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            request.ToReportComment(),
            cancellationToken);
        return TypedResults.Ok();
    }

    private void WriteClientLog(
        ClientLogLevel level,
        string eventName,
        string playerId,
        string screen,
        string status,
        string path)
    {
        switch (level)
        {
            case ClientLogLevel.Error:
                {
                    LogClientError(eventName, playerId, screen, status, path);
                    break;
                }

            case ClientLogLevel.Warn:
                {
                    LogClientWarning(eventName, playerId, screen, status, path);
                    break;
                }

            default:
                {
                    LogClientInformation(eventName, playerId, screen, status, path);
                    break;
                }
        }
    }

    [LoggerMessage(
        EventId = 1200,
        EventName = "Business.ClientLogInformation",
        Level = LogLevel.Information,
        Message = "Frontend event {EventName}; player {Player}; screen {Screen}; status {Status}; path {Path}")]
    private partial void LogClientInformation(
        string eventName,
        string player,
        string screen,
        string status,
        string path);

    [LoggerMessage(
        EventId = 1201,
        EventName = "Business.ClientLogWarning",
        Level = LogLevel.Warning,
        Message = "Frontend event {EventName}; player {Player}; screen {Screen}; status {Status}; path {Path}")]
    private partial void LogClientWarning(
        string eventName,
        string player,
        string screen,
        string status,
        string path);

    [LoggerMessage(
        EventId = 1202,
        EventName = "Business.ClientLogError",
        Level = LogLevel.Error,
        Message = "Frontend event {EventName}; player {Player}; screen {Screen}; status {Status}; path {Path}")]
    private partial void LogClientError(
        string eventName,
        string player,
        string screen,
        string status,
        string path);
}
