using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Common;
using Reki.Application.Telemetry.ReportCommentCommand;
using Reki.Application.Telemetry.SubmitAnalyticsCommand;
using Reki.Contracts.Telemetry;
using Reki.Domain.Posts;

namespace Reki.Api.Telemetry;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed partial class TelemetryController(
    ISender sender,
    IRekiContext rekiContext,
    ApiMapper mapper,
    ILogger<TelemetryController> logger) : ControllerBase
{
    [HttpPost("events", Name = "SubmitAnalytics")]
    [ActionName("SubmitAnalytics")]
    public async Task<NoContent> SubmitAnalyticsAsync(
        AnalyticsBatchRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new SubmitAnalytics(mapper.MapAnalytics(request)),
            cancellationToken);
        return TypedResults.NoContent();
    }

    [HttpPost("logs", Name = "SubmitClientLogs")]
    [ActionName("SubmitClientLogs")]
    public async Task<NoContent> SubmitClientLogsAsync(
        ClientLogBatchRequest request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContext.Player;
        foreach (var entry in request.Entries)
        {
            var properties = mapper.SanitizeProperties(entry.Props);
            properties.TryGetValue("status", out var status);
            properties.TryGetValue("path", out var path);
            WriteClientLog(
                entry.Level,
                entry.Event,
                player.Id.Value,
                entry.Screen ?? string.Empty,
                status ?? string.Empty,
                path ?? string.Empty);
        }

        return TypedResults.NoContent();
    }

    [HttpPost("reports", Name = "ReportComment")]
    [ActionName("ReportComment")]
    public async Task<Ok> ReportCommentAsync(
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ReportComment(
                CommentId.From(request.CommentId),
                request.Reason),
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