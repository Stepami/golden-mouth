using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Application.Player.Responses;
using Reki.Domain.Player.Streaks;

namespace Reki.Application.Player.Streaks.GetStreak;

internal sealed class PlayerStreakQueryHandler(IPlayerStreakRepository streaks, IMemoryCache cache, TimeProvider timeProvider) : IQueryHandler<PlayerStreakQuery, PlayerStreakResponse>
{
    public async ValueTask<PlayerStreakResponse> Handle(PlayerStreakQuery query, CancellationToken cancellationToken)
    {
        var key = CacheKeyPrefixes.PlayerStreak + query.PlayerId;
        if (cache.TryGetValue(key, out PlayerStreakResponse? cached) && cached is not null)
        {
            return cached;
        }

        var streak = await streaks.GetLatestAsync(query.PlayerId, cancellationToken);
        if (streak is null)
        {
            streak = PlayerStreak.New(query.PlayerId, timeProvider.GetUtcNow());
            await streaks.SaveAsync(streak, cancellationToken);
        }

        var response = streak.ToPlayerStreakResponse();
        cache.Set(key, response, TimeSpan.FromMinutes(1));
        return response;
    }
}
