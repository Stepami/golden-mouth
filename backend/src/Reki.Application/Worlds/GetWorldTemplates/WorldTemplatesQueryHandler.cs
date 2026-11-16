using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.GetWorldTemplates;

internal sealed class WorldTemplatesQueryHandler(IWorldRepository worldRepository, IMemoryCache cache) : IQueryHandler<WorldTemplatesQuery, IReadOnlyList<World>>
{
    public async ValueTask<IReadOnlyList<World>> Handle(WorldTemplatesQuery query, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKeyPrefixes.WorldTemplates, out IReadOnlyList<World>? templates) && templates is not null)
        {
            return templates;
        }

        var loaded = await worldRepository.GetWorldTemplatesAsync(cancellationToken);
        cache.Set(CacheKeyPrefixes.WorldTemplates, loaded, TimeSpan.FromDays(1));
        return loaded;
    }
}
