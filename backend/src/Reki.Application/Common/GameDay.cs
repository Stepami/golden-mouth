using Reki.Domain.Player;

namespace Reki.Application.Common;

public sealed class GameDay(TimeProvider timeProvider, GameBalance balance)
{
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    public DateTimeOffset PeriodStart
    {
        get
        {
            DateTimeOffset utcNow = timeProvider.GetUtcNow();
            return GetPeriodStart(utcNow);
        }
    }

    public PlayerProfile Normalize(PlayerProfile player)
    {
        DateTimeOffset periodStart = PeriodStart;
        return player.PostsPeriodStartedAtUtc == periodStart
            ? player
            : player.StartNewPeriod(periodStart);
    }

    private DateTimeOffset GetPeriodStart(DateTimeOffset utcNow)
    {
        DateTimeOffset localNow = utcNow.ToOffset(MoscowOffset);
        var start = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            balance.PostsResetTimeMoscow.Hour,
            balance.PostsResetTimeMoscow.Minute,
            balance.PostsResetTimeMoscow.Second,
            MoscowOffset);

        return (localNow < start ? start.AddDays(-1) : start).ToUniversalTime();
    }
}

