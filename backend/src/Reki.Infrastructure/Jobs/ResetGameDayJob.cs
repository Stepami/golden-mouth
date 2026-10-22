using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Balance;
using Reki.Application.Player;
using Reki.Application.Worlds;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

public sealed partial class ResetGameDayJob(
    IPlayerRepository players,
    IWorldRepository worlds,
    GameDayPeriod gameDayPeriod,
    IOptions<GameBalance> balance,
    ILogger<ResetGameDayJob> logger) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var periodStart = gameDayPeriod.PeriodStart;
        var growth = balance.Value.Growth;
        var restedBefore = periodStart.AddDays(-growth.RestedAfterSkippedDays);
        var playerCount = await players.ResetGameDayAsync(
            periodStart,
            balance.Value.FreePostsPerDay,
            balance.Value.PlusPostsPerDay,
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
