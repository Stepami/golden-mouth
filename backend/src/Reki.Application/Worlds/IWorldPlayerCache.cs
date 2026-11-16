using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public interface IWorldPlayerCache
{
    ValueTask<IReadOnlyList<World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken);

    void UpdateWorld(World world);

    void ClearPlayer(PlayerId playerId);
}
