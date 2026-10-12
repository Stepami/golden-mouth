using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class GetWorldHandler(IGameStore store, GameDay gameDay) : IRequestHandler<GetWorldQuery, World>
{
    public async ValueTask<World> Handle(GetWorldQuery request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var world = await store.GetWorldAsync(request.PlayerId, request.WorldId, cancellationToken)
                ?? throw new ResourceNotFoundException("World not found.");

            var normalized = gameDay.Normalize(world);
            if (ReferenceEquals(normalized, world) || await store.TryReplaceWorldAsync(normalized, world.Version, cancellationToken))
            {
                return normalized;
            }
        }

        throw new RequestConflictException("World changed concurrently.");
    }
}