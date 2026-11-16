using Microsoft.Extensions.Caching.Memory;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

internal sealed class WorldPlayerCache(IMemoryCache cache, IWorldRepository worlds) : IWorldPlayerCache
{
    public async ValueTask<IReadOnlyDictionary<WorldId, World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken)
    {
        var key = CacheKeyPrefixes.PlayerWorlds + playerId;
        if (!cache.TryGetValue(key, out IDictionary<WorldId, World>? saved) || saved is null)
        {
            saved = (await worlds.GetWorldsAsync(playerId, cancellationToken)).ToDictionary(world => world.Id);
            cache.Set(key, saved, TimeSpan.FromHours(1));
        }

        return saved.AsReadOnly();
    }

    public void UpdateWorld(World world)
    {
        var key = CacheKeyPrefixes.PlayerWorlds + world.Player.PlayerId;
        if (cache.TryGetValue(key, out IDictionary<WorldId, World>? saved) && saved is not null)
        {
            saved[world.Id] = world;
        }
    }

    public void ClearWorld(PlayerId playerId, WorldId worldId)
    {
        var key = CacheKeyPrefixes.PlayerWorlds + playerId;
        if (cache.TryGetValue(key, out IDictionary<WorldId, World>? saved) && saved is not null)
        {
            saved.Remove(worldId);
        }
    }

    public void ClearPlayer(PlayerId playerId) => cache.Remove(CacheKeyPrefixes.PlayerWorlds + playerId);
}
