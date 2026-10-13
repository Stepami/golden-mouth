using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class GetWorldHandler(IGameStore store) : IRequestHandler<GetWorldQuery, World>
{
    public async ValueTask<World> Handle(GetWorldQuery request, CancellationToken cancellationToken) =>
        await store.GetWorldAsync(request.PlayerId, request.WorldId, cancellationToken)
        ?? throw new ResourceNotFoundException("World not found.");
}
