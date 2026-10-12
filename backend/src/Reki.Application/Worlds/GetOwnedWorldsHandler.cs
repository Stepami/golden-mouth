using Mediator;
using Reki.Application.Common;
using Reki.Application.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class GetOwnedWorldsHandler(IGameStore store, ISender sender, GameDay gameDay) : IRequestHandler<GetOwnedWorldsQuery, IReadOnlyList<World>>
{
    public async ValueTask<IReadOnlyList<World>> Handle(GetOwnedWorldsQuery request, CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var worlds = await store.GetWorldsAsync(player.Id, cancellationToken);
        var result = new List<World>(worlds.Count);
        foreach (var world in worlds)
        {
            var normalized = gameDay.Normalize(world);
            if (!ReferenceEquals(normalized, world) &&
                !await store.TryReplaceWorldAsync(normalized, world.Version, cancellationToken))
            {
                normalized = await sender.Send(new GetWorldQuery(player.Id, world.Id), cancellationToken);
            }

            result.Add(normalized);
        }

        return result;
    }
}