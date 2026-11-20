using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Worlds;

namespace Reki.Application.Player.Challenges;

internal sealed class DailyProgress(IPlayerRepository players, TimeProvider timeProvider)
{
    public DateOnly Day => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    public async ValueTask ApplyAsync(
        PlayerId playerId,
        DateOnly day,
        WorldId worldId,
        IReadOnlyDictionary<DailyTaskType, long> changes,
        CancellationToken cancellationToken,
        StarId? starId = null)
    {
        if (Day != day)
        {
            return;
        }

        var player = await players.GetPlayerAsync(playerId, cancellationToken);
        if (player is null || player.DailyChallenge.Day != day)
        {
            return;
        }

        var previousVersion = player.Version;
        player.ApplyDailyChallengeProgress(worldId, changes, starId);
        if (player.Version != previousVersion)
        {
            await players.SaveDailyChallengeAsync(player, previousVersion, cancellationToken);
        }
    }
}
