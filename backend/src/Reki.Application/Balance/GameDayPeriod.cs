using Microsoft.Extensions.Options;

namespace Reki.Application.Balance;

public sealed class GameDayPeriod(TimeProvider timeProvider, IOptionsMonitor<GameBalance> balance)
{
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    public DateTimeOffset PeriodStart
    {
        get
        {
            var utcNow = timeProvider.GetUtcNow();
            return GetPeriodStart(utcNow);
        }
    }

    private DateTimeOffset GetPeriodStart(DateTimeOffset utcNow)
    {
        var currentBalance = balance.CurrentValue;
        var localNow = utcNow.ToOffset(MoscowOffset);
        var start = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            currentBalance.PostsResetTimeMoscow.Hour,
            currentBalance.PostsResetTimeMoscow.Minute,
            currentBalance.PostsResetTimeMoscow.Second,
            MoscowOffset);

        return (localNow < start ? start.AddDays(-1) : start).ToUniversalTime();
    }
}
