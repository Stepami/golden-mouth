using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Domain.Player;
using Reki.Domain.Player.Streaks;

namespace Reki.Application.Player.Streaks.UpdateStreakCommand;

internal sealed class UpdateStreakCommandHandler(
    IPlayerStreakRepository streaks,
    IPlayerRepository players,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    IOptionsMonitor<GameBalance> balance,
    TimeProvider timeProvider) : ICommandHandler<UpdateStreak, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(UpdateStreak command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var rewards = balance.CurrentValue.Bonuses;

        var (updated, changed) = await unitOfWork.ExecuteAsync(
            async token =>
            {
                var player = await players.GetPlayerAsync(command.PlayerId, token)
                             ?? throw new ResourceNotFoundException("Player not found.");
                var streak = await streaks.GetLatestAsync(command.PlayerId, token)
                             ?? PlayerStreak.New(command.PlayerId, now);
                var progress = streak.Progress(now);
                if (progress == PlayerStreakProgress.None)
                {
                    return (player, false);
                }

                await streaks.SaveAsync(streak, token);
                if (progress == PlayerStreakProgress.Stopped)
                {
                    var newStreak = PlayerStreak.New(command.PlayerId, now);
                    newStreak.Progress(now);
                    await streaks.SaveAsync(newStreak, token);
                }

                if (progress == PlayerStreakProgress.Consecutive &&
                    (rewards.StreakDays.Contains(streak.Count) ||
                     (streak.Count > rewards.StreakDays.Max() && streak.Count % rewards.StreakInterval == 0)))
                {
                    player.ReplenishCrystals(rewards.StreakCrystals);
                    await players.ReplacePlayerAsync(player, token);
                }

                return (player, true);
            },
            cancellationToken);
        if (changed)
        {
            cache.Remove(CacheKeyPrefixes.PlayerStreak + command.PlayerId);
        }

        return updated;
    }
}
