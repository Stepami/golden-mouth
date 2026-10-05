using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Game.Common;
using Reki.Application.Game.Player;
using Reki.Application.Game.Telemetry;
using Reki.Contracts.Game.Common;
using Reki.Contracts.Game.Telemetry;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Game.Telemetry;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed partial class TelemetryController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    GameMapper mapper,
    ILogger<TelemetryController> logger) : ControllerBase
{
    private readonly ILogger<TelemetryController> logger = logger;

    [HttpPost("events", Name = "SubmitAnalytics")]
    [ActionName("SubmitAnalytics")]
    public async Task<NoContent> SubmitAnalyticsAsync(
        AnalyticsBatchRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new SubmitAnalyticsCommand(telegramUser.User.Id, mapper.MapAnalytics(request)),
            cancellationToken);
        return TypedResults.NoContent();
    }

    [HttpPost("logs", Name = "SubmitClientLogs")]
    [ActionName("SubmitClientLogs")]
    public async Task<NoContent> SubmitClientLogsAsync(
        ClientLogBatchRequest request,
        CancellationToken cancellationToken)
    {
        PlayerView player = await sender.Send(
            new GetPlayerQuery(telegramUser.User.Id),
            cancellationToken);
        foreach (ClientLogEntryRequest entry in request.Entries)
        {
            IReadOnlyDictionary<string, string> properties = mapper.SanitizeProperties(entry.Props);
            properties.TryGetValue("status", out string? status);
            properties.TryGetValue("path", out string? path);
            WriteClientLog(
                entry.Level,
                entry.Event,
                player.Player.Id,
                entry.Screen ?? string.Empty,
                status ?? string.Empty,
                path ?? string.Empty);
        }

        return TypedResults.NoContent();
    }

    [HttpPost("reports", Name = "ReportComment")]
    [ActionName("ReportComment")]
    public async Task<Ok<OkResponse>> ReportCommentAsync(
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ReportCommentCommand(telegramUser.User.Id, request.CommentId, request.Reason),
            cancellationToken);
        return TypedResults.Ok(new OkResponse(true));
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
