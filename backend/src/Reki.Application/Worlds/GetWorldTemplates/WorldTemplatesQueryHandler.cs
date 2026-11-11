using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.GetWorldTemplates;

internal sealed class WorldTemplatesQueryHandler(IWorldRepository worldRepository) : IQueryHandler<WorldTemplatesQuery, IReadOnlyList<World>>
{
    public async ValueTask<IReadOnlyList<World>> Handle(WorldTemplatesQuery query, CancellationToken cancellationToken) =>
        await worldRepository.GetWorldTemplatesAsync(cancellationToken);
}
