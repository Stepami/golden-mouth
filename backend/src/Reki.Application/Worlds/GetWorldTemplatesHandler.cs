using Mediator;
using Reki.Application.Common;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class GetWorldTemplatesHandler(IGameStore store) : IRequestHandler<GetWorldTemplatesQuery, IReadOnlyList<World>>
{
    public async ValueTask<IReadOnlyList<World>> Handle(GetWorldTemplatesQuery request, CancellationToken cancellationToken) =>
        await store.GetWorldTemplatesAsync(cancellationToken);
}