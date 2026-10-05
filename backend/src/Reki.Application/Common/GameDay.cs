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
        if (player.PostsPeriodStartedAtUtc == periodStart)
        {
            return player;
        }

        int skippedDays = player.LastPostAtUtc is null
            ? 0
            : GetSkippedDays(GetPeriodStart(player.LastPostAtUtc.Value), periodStart);
        int restedPosts = skippedDays >= balance.Growth.RestedAfterSkippedDays
            ? balance.Growth.RestedFirstPosts
            : 0;
        return player.StartNewPeriod(periodStart, restedPosts);
    }

    private static int GetSkippedDays(DateTimeOffset lastPostPeriod, DateTimeOffset currentPeriod)
    {
        int elapsedPeriods = Math.Max(0, (int)(currentPeriod - lastPostPeriod).TotalDays);
        return Math.Max(0, elapsedPeriods - 1);
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

