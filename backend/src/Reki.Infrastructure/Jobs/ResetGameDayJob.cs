using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Player;
using Reki.Application.Worlds;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed partial class ResetGameDayJob(
    IPlayerRepository players,
    IWorldRepository worlds,
    GameDayPeriod gameDayPeriod,
    IOptionsMonitor<GameBalance> balance,
    ILogger<ResetGameDayJob> logger) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var currentBalance = balance.CurrentValue;
        var periodStart = gameDayPeriod.GetPeriodStart(currentBalance);
        var growth = currentBalance.Growth;
        var restedBefore = periodStart.AddDays(-growth.RestedAfterSkippedDays);
        var playerCount = await players.ResetGameDayAsync(
            periodStart,
            currentBalance.FreePostsPerDay,
            currentBalance.PlusPostsPerDay,
            cancellationToken);
        var worldCount = await worlds.ResetGameDayAsync(
            periodStart,
            restedBefore,
            growth.RestedFirstPosts,
            cancellationToken);

        LogResetCompleted(playerCount, worldCount, periodStart);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Game day reset completed: {Players} players, {Worlds} worlds, period {PeriodStart}")]
    private partial void LogResetCompleted(long players, long worlds, DateTimeOffset periodStart);
}
