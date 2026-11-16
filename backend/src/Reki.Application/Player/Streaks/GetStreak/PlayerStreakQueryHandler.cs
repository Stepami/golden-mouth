using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Application.Player.Responses;

namespace Reki.Application.Player.Streaks.GetStreak;

internal sealed class PlayerStreakQueryHandler(IPlayerStreakRepository streaks, IMemoryCache cache) : IQueryHandler<PlayerStreakQuery, PlayerStreakResponse?>
{
    public async ValueTask<PlayerStreakResponse?> Handle(PlayerStreakQuery query, CancellationToken cancellationToken)
    {
        var key = CacheKeyPrefixes.PlayerStreak + query.PlayerId;
        if (cache.TryGetValue(key, out PlayerStreakResponse? cached))
        {
            return cached;
        }

        var streak = await streaks.GetLatestAsync(query.PlayerId, cancellationToken);
        var response = streak is null ? null : new PlayerStreakResponse(streak.Id.Value, streak.StartedAt, streak.Count, streak.IsActive);
        cache.Set(key, response, TimeSpan.FromMinutes(1));
        return response;
    }
}
