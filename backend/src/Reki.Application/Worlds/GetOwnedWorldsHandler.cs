using Mediator;
using Reki.Application.Common;
using Reki.Application.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class GetOwnedWorldsHandler(IGameStore store, ISender sender) : IRequestHandler<GetOwnedWorldsQuery, IReadOnlyList<World>>
{
    public async ValueTask<IReadOnlyList<World>> Handle(GetOwnedWorldsQuery request, CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        return await store.GetWorldsAsync(player.Id, cancellationToken);
    }
}
