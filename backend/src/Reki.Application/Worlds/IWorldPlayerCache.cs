using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public interface IWorldPlayerCache
{
    ValueTask<IReadOnlyDictionary<WorldId, World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken);

    void UpdateWorld(World world);

    void ClearWorld(PlayerId playerId, WorldId worldId);

    void ClearPlayer(PlayerId playerId);
}
