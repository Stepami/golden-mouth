using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Reki.Application.Common;
using Reki.Infrastructure.Persistence;
using TickerQ.Utilities.Base;

namespace Reki.Infrastructure.Jobs;

public sealed partial class GameMaintenanceJobs(GameDay gameDay, RegisteredPlayersMetrics metrics, ILogger<GameMaintenanceJobs> logger)
{
    public const string ActivitySourceName = "Reki.Jobs";
    private static readonly ActivitySource Activities = new(ActivitySourceName);

    [TickerFunction("reset-game-day", cronExpression: "0 0 9 * * *", maxConcurrency: 1)]
    public Task ResetGameDayAsync(TickerFunctionContext context, CancellationToken cancellationToken) => ResetAsync(cancellationToken);

    [TickerFunction("refresh-registered-players", cronExpression: "0 * * * * *", maxConcurrency: 1)]
    public Task RefreshRegisteredPlayersCountAsync(TickerFunctionContext context, CancellationToken cancellationToken) => RefreshCountAsync(cancellationToken);

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        using var activity = Activities.StartActivity("reset-game-day");
        var periodStart = gameDay.PeriodStart;
        try
        {
            var result = await gameDay.ResetAsync(cancellationToken);
            activity?.SetTag("reki.reset.players", result.Players);
            activity?.SetTag("reki.reset.worlds", result.Worlds);
            LogResetCompleted(result.Players, result.Worlds, periodStart);
        }
        catch (Exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    public async Task RefreshCountAsync(CancellationToken cancellationToken)
    {
        using var activity = Activities.StartActivity("refresh-registered-players");
        try
        {
            await metrics.RefreshAsync(cancellationToken);
        }
        catch (Exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Game day reset completed: {Players} players, {Worlds} worlds, period {PeriodStart}")]
    private partial void LogResetCompleted(long players, long worlds, DateTimeOffset periodStart);
}
