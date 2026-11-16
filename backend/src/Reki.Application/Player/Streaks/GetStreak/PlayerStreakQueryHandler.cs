using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Domain.Player.Streaks;

namespace Reki.Application.Player.Streaks.GetStreak;

internal sealed class PlayerStreakQueryHandler(
    IPlayerStreakRepository streaks,
    IMemoryCache cache,
    TimeProvider timeProvider) : IQueryHandler<PlayerStreakQuery, PlayerStreak>
{
    public async ValueTask<PlayerStreak> Handle(
        PlayerStreakQuery query,
        CancellationToken cancellationToken)
    {
        var key = CacheKeyPrefixes.PlayerStreak + query.PlayerId;
        if (cache.TryGetValue(key, out PlayerStreak? cached) && cached is not null)
        {
            return cached;
        }

        var streak = await streaks.GetLatestAsync(query.PlayerId, cancellationToken);
        var startedAt = timeProvider.GetUtcNow();
        if (streak is null)
        {
            streak = PlayerStreak.New(query.PlayerId, startedAt);
            await streaks.SaveAsync(streak, cancellationToken);
        }

        var untilTomorrow = TimeSpan.FromDays(1) - startedAt.TimeOfDay - TimeSpan.FromTicks(1);
        cache.Set(key, streak, untilTomorrow);
        return streak;
    }
}
