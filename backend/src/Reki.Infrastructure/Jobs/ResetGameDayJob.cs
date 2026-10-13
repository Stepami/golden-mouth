using Microsoft.Extensions.Logging;
using Reki.Application.Common;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

public sealed partial class ResetGameDayJob(GameDay gameDay, ILogger<ResetGameDayJob> logger) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var periodStart = gameDay.PeriodStart;
        var result = await gameDay.ResetAsync(cancellationToken);
        LogResetCompleted(result.Players, result.Worlds, periodStart);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Game day reset completed: {Players} players, {Worlds} worlds, period {PeriodStart}")]
    private partial void LogResetCompleted(long players, long worlds, DateTimeOffset periodStart);
}
