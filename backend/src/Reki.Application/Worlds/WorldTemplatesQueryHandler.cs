using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class WorldTemplatesQueryHandler(IWorldRepository worldRepository) : IQueryHandler<WorldTemplatesQuery, IReadOnlyList<World>>
{
    public async ValueTask<IReadOnlyList<World>> Handle(WorldTemplatesQuery request, CancellationToken cancellationToken) =>
        await worldRepository.GetWorldTemplatesAsync(cancellationToken);
}
