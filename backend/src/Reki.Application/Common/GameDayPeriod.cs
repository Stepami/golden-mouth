using Microsoft.Extensions.Options;

namespace Reki.Application.Common;

public sealed class GameDayPeriod(TimeProvider timeProvider, IOptions<GameBalance> balance)
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
        var localNow = utcNow.ToOffset(MoscowOffset);
        var start = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            balance.Value.PostsResetTimeMoscow.Hour,
            balance.Value.PostsResetTimeMoscow.Minute,
            balance.Value.PostsResetTimeMoscow.Second,
            MoscowOffset);

        return (localNow < start ? start.AddDays(-1) : start).ToUniversalTime();
    }
}
