using Reki.Domain.Player;
using Reki.Domain.Player.Streaks;

namespace Reki.Application.Player.Streaks;

public interface IPlayerStreakRepository
{
    ValueTask<PlayerStreak?> GetLatestAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask SaveAsync(PlayerStreak streak, CancellationToken cancellationToken);

    ValueTask DeleteForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);
}
