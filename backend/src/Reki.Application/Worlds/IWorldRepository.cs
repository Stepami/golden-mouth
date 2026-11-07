using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public interface IWorldRepository
{
    ValueTask InsertWorldAsync(World world, CancellationToken cancellationToken);

    ValueTask<World?> GetWorldAsync(
        PlayerId playerId,
        WorldId worldId,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<World>> GetWorldTemplatesAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<WorldId>> GetWorldIdsAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask<bool> TryReplaceWorldAsync(World world, int expectedVersion, CancellationToken cancellationToken);

    ValueTask DeleteWorldsAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask<long> ResetGameDayAsync(
        DateTimeOffset periodStart,
        DateTimeOffset restedBefore,
        int restedPosts,
        CancellationToken cancellationToken);
}