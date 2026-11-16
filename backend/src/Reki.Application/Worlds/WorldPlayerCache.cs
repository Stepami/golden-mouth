using Microsoft.Extensions.Caching.Memory;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

internal sealed class WorldPlayerCache(IMemoryCache cache, IWorldRepository worlds) : IWorldPlayerCache
{
    public async ValueTask<IReadOnlyList<World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken)
    {
        var key = CacheKeyPrefixes.PlayerWorlds + playerId;
        if (cache.TryGetValue(key, out World[]? saved) && saved is not null)
        {
            return saved.Select(Copy).ToArray();
        }

        var loaded = await worlds.GetWorldsAsync(playerId, cancellationToken);
        cache.Set(key, loaded.Select(Copy).ToArray(), TimeSpan.FromHours(1));
        return loaded;
    }

    public void UpdateWorld(World world)
    {
        var key = CacheKeyPrefixes.PlayerWorlds + world.Player.PlayerId;
        if (cache.TryGetValue(key, out World[]? saved) && saved is not null)
        {
            var snapshot = Copy(world);
            var updated = saved.Select(item => item.Id == world.Id ? snapshot : item).ToList();
            if (saved.All(item => item.Id != world.Id))
            {
                updated.Add(snapshot);
            }

            cache.Set(key, updated.ToArray(), TimeSpan.FromHours(1));
        }
    }

    public void ClearPlayer(PlayerId playerId) => cache.Remove(CacheKeyPrefixes.PlayerWorlds + playerId);

    private static World Copy(World world) => new(
        world.Id,
        world.Interface,
        world.Player,
        world.TemplateId,
        world.Difficulty,
        world.IsArchived,
        world.Roles,
        world.Voices,
        world.PostIdeas,
        world.DemoPosts,
        world.Rules,
        world.Stars,
        world.Version);
}
