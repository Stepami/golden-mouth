using Microsoft.Extensions.Options;
using Reki.Domain.Player;

namespace Reki.Application.Common;

public sealed class GameDay(TimeProvider timeProvider, IOptions<GameBalance> balance)
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

    public PlayerProfile Normalize(PlayerProfile player)
    {
        var periodStart = PeriodStart;
        if (player.PostsPeriodStartedAtUtc == periodStart)
        {
            return player;
        }

        var skippedDays = player.LastPostAtUtc is null
            ? 0
            : GetSkippedDays(GetPeriodStart(player.LastPostAtUtc.Value), periodStart);
        var restedPosts = skippedDays >= balance.Value.Growth.RestedAfterSkippedDays
            ? balance.Value.Growth.RestedFirstPosts
            : 0;
        return player.StartNewPeriod(periodStart, restedPosts);
    }

    private static int GetSkippedDays(DateTimeOffset lastPostPeriod, DateTimeOffset currentPeriod)
    {
        var elapsedPeriods = Math.Max(0, (int)(currentPeriod - lastPostPeriod).TotalDays);
        return Math.Max(0, elapsedPeriods - 1);
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

