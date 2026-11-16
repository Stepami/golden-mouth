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
        if (streak is null)
        {
            streak = PlayerStreak.New(query.PlayerId, timeProvider.GetUtcNow());
            await streaks.SaveAsync(streak, cancellationToken);
        }

        cache.Set(key, streak, TimeSpan.FromDays(1));
        return streak;
    }
}
